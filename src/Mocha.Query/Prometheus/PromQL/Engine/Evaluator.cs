// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Extensions;
using Mocha.Core.Models.Metrics;
using Mocha.Query.Prometheus.PromQL.Ast;
using Mocha.Query.Prometheus.PromQL.Exceptions;
using Mocha.Query.Prometheus.PromQL.Values;
using Expression = Mocha.Query.Prometheus.PromQL.Ast.Expression;
using StringLiteral = Mocha.Query.Prometheus.PromQL.Ast.StringLiteral;

namespace Mocha.Query.Prometheus.PromQL.Engine;

// TODO: Use ArrayPool
internal class Evaluator
{
    public long StartTimestampUnixSec { get; init; }

    public long EndTimestampUnixSec { get; init; }

    public TimeSpan Interval { get; init; }

    public int MaxSamples { get; init; }

    private int _currentSamples;

    public MatrixResult Eval(Expression expr)
    {
        var numSteps = (int)((EndTimestampUnixSec - StartTimestampUnixSec) / Interval.TotalSeconds) + 1;
        MatrixResult result;
        switch (expr)
        {
            case AggregateExpression aggregate:
                {
                    if (aggregate.Parameter is StringLiteral stringLiteral)
                    {
                        result = RangeEval(
                            (args, enh) =>
                            {
                                if (args.Length == 0)
                                {
                                    return [];
                                }

                                var aggregationResult = Aggregation(aggregate.Op,
                                    aggregate.Grouping,
                                    aggregate.Without,
                                    stringLiteral.Value,
                                    (VectorResult)args[0],
                                    enh);
                                return aggregationResult;
                            },
                            aggregate.Expression);
                        return result;
                    }

                    result = RangeEval(
                        (args, enh) =>
                        {
                            if (args.Length == 0)
                            {
                                return [];
                            }

                            double? param = null;
                            if (aggregate.Parameter is not null)
                            {
                                param = ((NumberLiteral)aggregate.Parameter).Value;
                            }

                            var aggregationResult = Aggregation(
                                aggregate.Op,
                                aggregate.Grouping,
                                aggregate.Without,
                                param,
                                (VectorResult)args[0],
                                enh);
                            return aggregationResult;
                        },
                        aggregate.Expression);
                    return result;
                }

            case Call call:
                {
                    if (call.Func.Name == FunctionName.Timestamp)
                    {
                        // Matrix evaluation always returns the evaluation time,
                        // so this function needs special handling when given
                        // a vector selector.
                        if (call.Args[0] is VectorSelector vectorSelector)
                        {
                            result = RangeEval((_, enh) =>
                            {
                                var funcCallResult = call.Func.Call(
                                    [VectorSelector(vectorSelector, enh.TimestampUnixSec)],
                                    call.Args,
                                    enh);
                                return funcCallResult;
                            });
                            return result;
                        }
                    }

                    // Check if the function has a matrix argument.
                    var matrixArgIndex = Array.FindIndex(call.Args, e => e is MatrixSelector);
                    var matrixArg = matrixArgIndex != -1;

                    if (!matrixArg)
                    {
                        // Does not have a matrix argument.
                        result = RangeEval((values, enh) => call.Func.Call(values, call.Args, enh), call.Args);
                        return result;
                    }

                    var inArgs = new IParseResult[call.Args.Length];
                    // Evaluate any non-matrix arguments.
                    var otherArgs = new MatrixResult[call.Args.Length];
                    var otherInArgs = new VectorResult[call.Args.Length];
                    for (var i = 0; i < call.Args.Length; i++)
                    {
                        if (i == matrixArgIndex)
                        {
                            continue;
                        }

                        otherArgs[i] = Eval(call.Args[i]);
                        otherInArgs[i] = [];
                        inArgs[i] = otherInArgs[i];
                    }

                    var matrixSelector = (MatrixSelector)call.Args[matrixArgIndex];
                    result = new MatrixResult(matrixSelector.Series.Count());
                    var selectorOffsetSeconds = (long)matrixSelector.Offset.TotalSeconds;
                    var selectorRangeSeconds = matrixSelector.Range.TotalSeconds;
                    var stepRangeSeconds = (long)Math.Min(selectorRangeSeconds, Interval.TotalSeconds);

                    // Reuse objects across steps to save memory allocations.
                    // TODO: use ArrayPool
                    var inMatrix = new MatrixResult(1) { new Series { Metric = Labels.Empty, Points = [] } };
                    inArgs[matrixArgIndex] = inMatrix;
                    var enh = new EvalNodeHelper { Output = new VectorResult(1) };

                    // Process all the calls for one time series at a time.
                    foreach (var timeSeries in matrixSelector.Series)
                    {
                        var series = new Series
                        {
                            Metric = timeSeries.Labels.DropMetricName(),
                            // TODO: use ArrayPool
                            Points = new List<DoublePoint>(numSteps)
                        };

                        inMatrix[0].Metric = timeSeries.Labels;
                        var step = -1;
                        var refTimeStart = StartTimestampUnixSec - selectorOffsetSeconds;
                        var refTimeEnd = EndTimestampUnixSec - selectorOffsetSeconds;
                        for (var ts = refTimeStart; ts <= refTimeEnd; ts += stepRangeSeconds)
                        {
                            step++;
                            // Set the non-matrix arguments.
                            // They are scalar, so it is safe to use the step number
                            // when looking up the argument, as there will be no gaps.
                            for (var i = 0; i < call.Args.Length; i++)
                            {
                                if (i != matrixArgIndex)
                                {
                                    otherInArgs[i][0].Point.Value = otherArgs[i][0].Points[step].Value;
                                }
                            }

                            var maxTs = ts;
                            var minTs = maxTs - selectorRangeSeconds;
                            // Evaluate the matrix selector for this series for this step.
                            // TODO: optimize enumeration
                            var points = timeSeries.Samples
                                .Where(s => s.TimestampUnixSec > minTs && s.TimestampUnixSec <= maxTs)
                                .Select(s =>
                                    new DoublePoint { TimestampUnixSec = s.TimestampUnixSec, Value = s.Value })
                                .ToList();

                            if (points.Count <= 0)
                            {
                                continue;
                            }

                            inMatrix[0].Points = points;
                            enh.TimestampUnixSec = ts;
                            enh.Output.Clear();
                            // Make the function call.
                            var callResult = call.Func.Call(inArgs, call.Args, enh);
                            if (callResult.Count > 0)
                            {
                                series.Points.Add(new DoublePoint
                                {
                                    TimestampUnixSec = ts,
                                    Value = callResult[0].Point.Value
                                });
                            }
                        }

                        if (series.Points.Count <= 0)
                        {
                            continue;
                        }

                        if (_currentSamples < MaxSamples)
                        {
                            result.Add(series);
                            _currentSamples += series.Points.Count;
                        }
                        else
                        {
                            throw new TooManySamplesException();
                        }
                    }

                    if (result.ContainsSameLabelSet())
                    {
                        throw new InvalidOperationException("Vector cannot contain metrics with the same labelset");
                    }

                    return result;
                }

            case UnaryExpression unary:
                {
                    result = Eval(unary.Expression);
                    switch (unary.Operator)
                    {
                        case Operator.Add:
                            return result;
                        case Operator.Sub:
                            foreach (var series in result)
                            {
                                series.Metric = series.Metric.DropMetricName();
                                foreach (var point in series.Points)
                                {
                                    point.Value = -point.Value;
                                }
                            }

                            if (result.ContainsSameLabelSet())
                            {
                                throw new InvalidOperationException("Matrix cannot contain metrics with the same labelset");
                            }

                            return result;
                        default:
                            throw new NotSupportedException($"Unary operation {unary.Operator} is not supported.");
                    }
                }

            case BinaryExpression binary:
                {
                    switch (binary.LHS.Type, binary.RHS.Type)
                    {
                        case (PrometheusValueType.Scalar, PrometheusValueType.Scalar):
                            result = RangeEval((values, enh) =>
                                {
                                    var lhsValue = ((VectorResult)values[0])[0].Point.Value;
                                    var rhsValue = ((VectorResult)values[1])[0].Point.Value;
                                    var value = ScalarBinaryOp(binary.Op,
                                        lhsValue,
                                        rhsValue);
                                    enh.Output.Add(new Sample
                                    {
                                        Metric = Labels.Empty,
                                        Point = new DoublePoint { Value = value }
                                    });
                                    return enh.Output;
                                },
                                binary.LHS, binary.RHS);
                            return result;
                        case (PrometheusValueType.Vector, PrometheusValueType.Vector):
                            var matching = binary.VectorMatching ??
                                           throw new InvalidOperationException("Vector operations must have matching");
                            result = RangeEval((values, enh) =>
                                {
                                    var lhsValue = (VectorResult)values[0];
                                    var rhsValue = (VectorResult)values[1];
                                    return binary.Op switch
                                    {
                                        Operator.And => VectorAnd(lhsValue, rhsValue, matching, enh),
                                        Operator.Or => VectorOr(lhsValue, rhsValue, matching, enh),
                                        Operator.Unless => VectorUnless(lhsValue, rhsValue, matching, enh),
                                        _ => VectorBinaryOp(binary.Op, lhsValue, rhsValue, matching, binary.ReturnBool, enh)
                                    };
                                },
                                binary.LHS, binary.RHS);
                            return result;

                        case (PrometheusValueType.Vector, PrometheusValueType.Scalar):
                            result = RangeEval((values, enh) =>
                                {
                                    var lhsValue = (VectorResult)values[0];
                                    var rhsValue = new ScalarResult { Value = ((VectorResult)values[1])[0].Point.Value };
                                    return VectorScalarBinaryOp(
                                        binary.Op,
                                        lhsValue,
                                        rhsValue,
                                        false,
                                        binary.ReturnBool,
                                        enh);
                                },
                                binary.LHS, binary.RHS);
                            return result;

                        case (PrometheusValueType.Scalar, PrometheusValueType.Vector):
                            result = RangeEval((values, enh) =>
                                {
                                    var lhsValue = (VectorResult)values[1];
                                    var rhsValue = new ScalarResult { Value = ((VectorResult)values[0])[0].Point.Value };
                                    return VectorScalarBinaryOp(
                                        binary.Op,
                                        lhsValue,
                                        rhsValue,
                                        true,
                                        binary.ReturnBool,
                                        enh);
                                },
                                binary.LHS, binary.RHS);
                            return result;
                        case (_, _):
                            throw new NotSupportedException(
                                $"Binary operation between {binary.LHS.Type} and {binary.RHS.Type} is not supported.");
                    }
                }

            case NumberLiteral numberLiteral:
                return RangeEval((_, enh) =>
                {
                    enh.Output.Add(new Sample
                    {
                        Metric = Labels.Empty,
                        Point = new DoublePoint { Value = numberLiteral.Value }
                    });
                    return enh.Output;
                });

            case VectorSelector vectorSelector:
                {
                    result = new MatrixResult(vectorSelector.Series.Count());
                    var offsetSeconds = (long)vectorSelector.Offset.TotalSeconds;
                    foreach (var timeSeries in vectorSelector.Series)
                    {
                        var series = new Series
                        {
                            Metric = new Labels(timeSeries.Labels),
                            // TODO: use ArrayPool
                            Points = new List<DoublePoint>(numSteps)
                        };
                        var intervalSeconds = (long)Interval.TotalSeconds;
                        var refTimeStart = StartTimestampUnixSec - offsetSeconds;
                        var refTimeEnd = EndTimestampUnixSec - offsetSeconds;
                        using var enumerator = timeSeries.Samples.Reverse().GetEnumerator();

                        var currentSample = enumerator.MoveNext() ? enumerator.Current : null;
                        for (var ts = refTimeEnd; ts >= refTimeStart && currentSample != null; ts -= intervalSeconds)
                        {
                            while (currentSample.TimestampUnixSec > ts)
                            {
                                var noMoreSamples = !enumerator.MoveNext();
                                if (noMoreSamples)
                                {
                                    break;
                                }

                                currentSample = enumerator.Current;
                            }

                            if (_currentSamples < MaxSamples)
                            {
                                series.Points.Add(
                                    new DoublePoint { TimestampUnixSec = ts, Value = currentSample.Value });
                                _currentSamples++;
                            }
                            else
                            {
                                throw new TooManySamplesException();
                            }
                        }

                        result.Add(series);
                    }

                    result.Reverse();
                    return result;
                }
            case MatrixSelector matrixSelector:
                {
                    if (StartTimestampUnixSec != EndTimestampUnixSec)
                    {
                        throw new NotSupportedException("Cannot do range evaluation of matrix selector");
                    }

                    var offsetSeconds = (long)matrixSelector.Offset.TotalSeconds;
                    var maxTs = StartTimestampUnixSec - offsetSeconds;
                    var minTs = maxTs - (long)matrixSelector.Range.TotalSeconds;

                    result = new MatrixResult(matrixSelector.Series.Count());
                    foreach (var timeSeries in matrixSelector.Series)
                    {
                        var series = new Series
                        {
                            Metric = new Labels(timeSeries.Labels),
                            Points = timeSeries.Samples
                                .Where(p => p.TimestampUnixSec > minTs && p.TimestampUnixSec <= maxTs)
                                .Select(p => new DoublePoint { TimestampUnixSec = p.TimestampUnixSec, Value = p.Value })
                                .ToList()
                        };
                        _currentSamples += series.Points.Count;

                        if (_currentSamples > MaxSamples)
                        {
                            throw new TooManySamplesException();
                        }

                        result.Add(series);
                    }

                    return result;
                }

            default:
                throw new NotSupportedException($"Expression type {expr.GetType()} is not supported.");
        }
    }


    /// <summary>
    /// Evaluates the given expressions, and then for each step calls
    /// the given function with the values computed for each expression at that step.
    /// </summary>
    /// <param name="func">The function to call for each step.</param>
    /// <param name="expressions">The expressions to evaluate.</param>
    /// <returns>The combination into time series of all the function call results.</returns>
    private MatrixResult RangeEval(
        Func<IParseResult[], EvalNodeHelper, VectorResult> func,
        params Expression[] expressions)
    {
        // calculate the number of steps, so we can pre-allocate the output matrix.
        var numSteps = (int)((EndTimestampUnixSec - StartTimestampUnixSec) / Interval.TotalSeconds) + 1;
        var matrixes = new MatrixResult[expressions.Length];
        // var originalMatrixes = new MatrixResult[expressions.Length];
        var orginalNumSamples = _currentSamples;

        for (var i = 0; i < expressions.Length; i++)
        {
            var expr = expressions[i] ?? throw new NotSupportedException("Expression is null");
            if (expr.Type == PrometheusValueType.String)
            {
                continue;
            }

            matrixes[i] = Eval(expr);
        }

        var vectors = new VectorResult[expressions.Length]; // Input vectors for the function.
        var args = new IParseResult[expressions.Length]; // Arguments to the function.

        // Create an output vector that is as big as the input matrix with
        // the most time series.
        var maxCapacity = 1;
        for (var i = 0; i < expressions.Length; i++)
        {
            var capacity = matrixes[i].Count;
            vectors[i] = new VectorResult(capacity);
            maxCapacity = Math.Max(maxCapacity, capacity);
        }

        var enh = new EvalNodeHelper { Output = new VectorResult(maxCapacity) };
        var seriess = new Dictionary<Labels, Series>();
        var tempNumSamples = _currentSamples;
        var intervalSeconds = (long)Interval.TotalSeconds;
        for (var ts = StartTimestampUnixSec; ts <= EndTimestampUnixSec; ts += intervalSeconds)
        {
            // Reset number of samples in memory after each timestamp.
            _currentSamples = tempNumSamples;
            // Gather input vectors for this timestamp.
            for (var i = 0; i < expressions.Length; i++)
            {
                vectors[i].Clear();
                var si = 0;
                foreach (var series in matrixes[i])
                {
                    foreach (var point in series.Points)
                    {
                        if (point.TimestampUnixSec == ts)
                        {
                            if (_currentSamples < MaxSamples)
                            {
                                vectors[i].Add(new Sample { Metric = series.Metric, Point = point });
                                // Move input vectors forward so we don't have to re-scan the same
                                // past points at the next step.
                                matrixes[i][si].Points = series.Points[1..];
                                _currentSamples++;
                            }
                            else
                            {
                                throw new TooManySamplesException();
                            }
                        }

                        break;
                    }

                    si++;
                }

                args[i] = vectors[i];
            }

            // Make the function call.
            enh.TimestampUnixSec = ts;
            // Reuse result vector.
            enh.Output.Clear();
            var result = func(args, enh);

            if (result.ContainsSameLabelSet())
            {
                throw new InvalidOperationException("Vector cannot contain metrics with the same labelset");
            }

            _currentSamples += result.Count;
            // When we reset currentSamples to tempNumSamples during the next iteration of the loop it also
            // needs to include the samples from	 the result here, as they're still in memory.
            tempNumSamples += result.Count;
            if (_currentSamples > MaxSamples)
            {
                throw new TooManySamplesException();
            }

            // If this could be an instant query, shortcut so as not to change sort order.
            if (EndTimestampUnixSec == StartTimestampUnixSec)
            {
                var matrix = new MatrixResult(result.Count);
                foreach (var sample in result)
                {
                    sample.Point.TimestampUnixSec = ts;
                    var series = new Series { Metric = sample.Metric, Points = [sample.Point] };
                    matrix.Add(series);
                }

                _currentSamples = orginalNumSamples + matrix.TotalSamples();
                return matrix;
            }

            // Add samples in output vector to output series.
            foreach (var sample in result)
            {
                if (!seriess.TryGetValue(sample.Metric, out var series))
                {
                    series = new Series { Metric = sample.Metric, Points = new List<DoublePoint>(maxCapacity) };
                    seriess[sample.Metric] = series;
                }

                sample.Point.TimestampUnixSec = ts;
                series.Points.Add(sample.Point);
            }
        }

        // Assemble the output matrix. By the time we get here we know we don't have too many samples.
        var outputMatrix = new MatrixResult(seriess.Count);
        outputMatrix.AddRange(seriess.Values);

        _currentSamples = orginalNumSamples + outputMatrix.TotalSamples();
        return outputMatrix;
    }

    private VectorResult VectorAnd(VectorResult lhs, VectorResult rhs, VectorMatching matching, EvalNodeHelper enh)
    {
        if (matching.Cardinality != VectorMatchCardinality.ManyToMany)
        {
            throw new InvalidOperationException("Set operations must only use many-to-many matching");
        }

        // The set of signatures for the right-hand side Vector.
        var rightSignatures =
            new HashSet<Labels>(
                rhs.Select(s => s.Metric.MatchLabels(matching.On, matching.MatchingLabels)));
        // Add all rhs samples to a map so we can easily find matches later.
        foreach (var leftSeries in lhs)
        {
            var leftSignatures = leftSeries.Metric.MatchLabels(matching.On, matching.MatchingLabels);
            if (rightSignatures.Contains(leftSignatures))
            {
                enh.Output.Add(leftSeries);
            }
        }

        return enh.Output;
    }

    private VectorResult VectorOr(VectorResult lhs, VectorResult rhs, VectorMatching matching, EvalNodeHelper enh)
    {
        if (matching.Cardinality != VectorMatchCardinality.ManyToMany)
        {
            throw new InvalidOperationException("Set operations must only use many-to-many matching");
        }

        var leftSignatures = new HashSet<Labels>();
        // Add everything from the left-hand-side Vector.
        foreach (var leftSeries in lhs)
        {
            leftSignatures.Add(leftSeries.Metric.MatchLabels(matching.On, matching.MatchingLabels));
            enh.Output.Add(leftSeries);
        }

        // Add all right-hand side elements which have not been added from the left-hand side.
        foreach (var rightSeries in rhs)
        {
            var rightSignature = rightSeries.Metric.MatchLabels(matching.On, matching.MatchingLabels);
            if (!leftSignatures.Contains(rightSignature))
            {
                enh.Output.Add(rightSeries);
            }
        }

        return enh.Output;
    }

    private VectorResult VectorUnless(VectorResult lhs, VectorResult rhs, VectorMatching matching, EvalNodeHelper enh)
    {
        if (matching.Cardinality != VectorMatchCardinality.ManyToMany)
        {
            throw new InvalidOperationException("Set operations must only use many-to-many matching");
        }

        var rightSignatures = new HashSet<Labels>(
            rhs.Select(s => s.Metric.MatchLabels(matching.On, matching.MatchingLabels)));
        foreach (var leftSeries in lhs)
        {
            var leftSignature = leftSeries.Metric.MatchLabels(matching.On, matching.MatchingLabels);
            if (!rightSignatures.Contains(leftSignature))
            {
                enh.Output.Add(leftSeries);
            }
        }

        return enh.Output;
    }

    // Evaluates a binary operation between two Vectors, excluding set operators.
    private VectorResult VectorBinaryOp(
        Operator op,
        VectorResult lhs,
        VectorResult rhs,
        VectorMatching matching,
        bool returnBool,
        EvalNodeHelper enh)
    {
        if (matching.Cardinality == VectorMatchCardinality.ManyToMany)
        {
            throw new NotSupportedException("Many-to-many only allowed for set operators");
        }

        // The control flow below handles one-to-one or many-to-one matching.
        // For one-to-many, swap sidedness and account for the swap when calculating
        // values.
        if (matching.Cardinality == VectorMatchCardinality.OneToMany)
        {
            (lhs, rhs) = (rhs, lhs);
        }

        // Add all rhs samples to a Dictionary so we can easily find matches later.
        var rightSignatures = new Dictionary<Labels, Sample>();
        foreach (var rightSeries in rhs)
        {
            if (rightSignatures.TryGetValue(rightSeries.Metric, out var rightSignature))
            {
                var oneSide = matching.Cardinality == VectorMatchCardinality.OneToMany ? "left" : "right";
                var matchedLabels = rightSeries.Metric.MatchLabels(matching.On, matching.MatchingLabels);
                // Many-to-many matching not allowed.
                throw new InvalidOperationException(
                    $"Found duplicate series for the match group {matchedLabels} on the {oneSide} hand-side of the operation: [{rightSeries.Metric}, {rightSignature.Metric}]; many-to-many matching not allowed: matching labels must be unique on one side");
            }

            rightSignatures[rightSeries.Metric] = rightSeries;
        }

        // Tracks the match-signature. For one-to-one operations the value is nil. For many-to-one
        // the value is a set of signatures to detect duplicated result elements.
        var matchedSignatures = enh.MatchedSignatures;
        if (matchedSignatures == null)
        {
            enh.MatchedSignatures = matchedSignatures = new Dictionary<Labels, HashSet<Labels>>();
        }
        else
        {
            matchedSignatures.Clear();
        }

        // For all lhs samples find a respective rhs sample and perform
        // the binary operation.
        foreach (var leftSample in lhs)
        {
            if (rightSignatures.TryGetValue(leftSample.Metric, out var rightSample) == false)
            {
                continue;
            }

            // Account for potentially swapped sidedness.
            var (lhsValue, rhsValue) = matching.Cardinality switch
            {
                VectorMatchCardinality.OneToMany => (rightSample.Point.Value, leftSample.Point.Value),
                _ => (leftSample.Point.Value, rightSample.Point.Value)
            };

            var (value, keep) = VectorElementBinaryOp(op, lhsValue, rhsValue);
            if (returnBool)
            {
                value = keep ? 1 : 0;
            }
            else if (!keep)
            {
                continue;
            }

            var metric = ResultMetric(leftSample.Metric, rightSample.Metric, op, matching, enh);
            var exists = matchedSignatures.TryGetValue(metric, out var insertedSignatures);
            if (matching.Cardinality == VectorMatchCardinality.OneToOne)
            {
                if (exists)
                {
                    throw new InvalidOperationException(
                        "Multiple matches for labels: many-to-one matching must be explicit (group_left/group_right)");
                }

                // Set existence to true.
                matchedSignatures[metric] = null!;
            }
            else
            {
                // In many-to-one matching the grouping labels have to ensure a unique metric
                // for the result Vector. Check whether those labels have already been added for
                // the same matching labels.
                if (!exists)
                {
                    insertedSignatures = new HashSet<Labels>();
                    matchedSignatures[leftSample.Metric] = insertedSignatures;
                }
                else if (insertedSignatures!.Contains(metric))
                {
                    throw new InvalidOperationException(
                        "Multiple matches for labels: grouping labels must ensure unique matches");
                }

                insertedSignatures.Add(metric);
            }

            enh.Output.Add(new Sample { Metric = metric, Point = new DoublePoint { Value = value } });
        }

        return enh.Output;

        // Returns the metric for the given sample(s) based on the Vector binary operation and the matching options.
        static Labels ResultMetric(Labels lhs, Labels rhs, Operator op, VectorMatching matching, EvalNodeHelper enh)
        {
            enh.ResultMetric ??= [];
            if (enh.ResultMetric.TryGetValue((lhs, rhs), out var result))
            {
                return result;
            }

            var labelsBuilder = Labels.Builder(lhs);
            if (ShouldDropMetricName(op))
            {
                labelsBuilder.Remove(Labels.MetricName);
            }

            if (matching.Cardinality == VectorMatchCardinality.OneToOne)
            {
                if (matching.On)
                {
                    labelsBuilder.RemoveRange(lhs
                        .Where(l => matching.MatchingLabels.Contains(l.Key) == false)
                        .Select(l => l.Key));
                }
                else
                {
                    labelsBuilder.RemoveRange(matching.MatchingLabels);
                }
            }

            foreach (var labelName in matching.Include)
            {
                // Included labels from the `group_x` modifier are taken from the "one"-side.
                if (rhs.TryGetValue(labelName, out var value) && string.IsNullOrEmpty(value) == false)
                {
                    labelsBuilder.Add(labelName, value);
                }
                else
                {
                    labelsBuilder.Remove(labelName);
                }
            }

            result = labelsBuilder.Build();
            enh.ResultMetric[(lhs, rhs)] = result;
            return result;
        }
    }

    // Evaluates a binary operation between a Vector and a Scalar.
    VectorResult VectorScalarBinaryOp(
        Operator op,
        VectorResult lhs,
        ScalarResult rhs,
        bool swap,
        bool returnBool,
        EvalNodeHelper enh)
    {
        foreach (var lhsSample in lhs)
        {
            var leftValue = lhsSample.Point.Value;
            var rightValue = rhs.Value;
            // lhs always contains the Vector. If the original position was different
            // swap for calculating the value.
            if (swap)
            {
                (leftValue, rightValue) = (rightValue, leftValue);
            }

            var (value, keep) = VectorElementBinaryOp(op, leftValue, rightValue);
            // Catch cases where the scalar is the LHS in a scalar-vector comparison operation.
            // We want to always keep the vector element value as the output value, even if it's on the RHS.

            if (op.IsComparisonOperator() && swap)
            {
                value = rightValue;
            }

            if (returnBool)
            {
                value = keep ? 1.0 : 0.0;

                keep = true;
            }

            if (keep)
            {
                lhsSample.Point.Value = value;
                if (ShouldDropMetricName(op) || returnBool)
                {
                    lhsSample.Metric = lhsSample.Metric.DropMetricName();
                }

                enh.Output.Add(lhsSample);
            }
        }

        return enh.Output;
    }

    // Evaluates a binary operation between two Scalars.
    private double ScalarBinaryOp(Operator op, double lhs, double rhs)
    {
        var result = op switch
        {
            Operator.Add => lhs + rhs,
            Operator.Sub => lhs - rhs,
            Operator.Mul => lhs * rhs,
            Operator.Div => lhs / rhs,
            Operator.Mod => lhs % rhs,
            Operator.Pow => Math.Pow(lhs, rhs),
            Operator.Eql => Convert.ToInt32(Math.Abs(lhs - rhs) <= 0),
            Operator.Neq => Convert.ToInt32(Math.Abs(lhs - rhs) > 0),
            Operator.Gtr => Convert.ToInt32(lhs > rhs),
            Operator.Lss => Convert.ToInt32(lhs < rhs),
            Operator.Gte => Convert.ToInt32(lhs >= rhs),
            Operator.Lte => Convert.ToInt32(lhs <= rhs),
            _ => throw new NotSupportedException($"Operator {op} not allowed for Scalar operations")
        };
        return result;
    }

    // Evaluates a binary operation between two Vector elements.
    private (double Result, bool Keep) VectorElementBinaryOp(
        Operator op,
        double lhs,
        double rhs)
    {
        var (result, keep) = op switch
        {
            Operator.Add => (lhs + rhs, true),
            Operator.Sub => (lhs - rhs, true),
            Operator.Mul => (lhs * rhs, true),
            Operator.Div => (lhs / rhs, true),
            Operator.Mod => (lhs % rhs, true),
            Operator.Pow => (Math.Pow(lhs, rhs), true),
            Operator.Eql => (lhs, Math.Abs(lhs - rhs) <= 0),
            Operator.Neq => (lhs, Math.Abs(lhs - rhs) > 0),
            Operator.Gtr => (lhs, lhs > rhs),
            Operator.Lss => (lhs, lhs < rhs),
            Operator.Gte => (lhs, lhs >= rhs),
            Operator.Lte => (lhs, lhs <= rhs),
            _ => throw new NotSupportedException($"Operator {op} not allowed for operations between Vectors")
        };

        return (result, keep);
    }

    private VectorResult Aggregation(
        AggregationOp op,
        HashSet<string> grouping,
        bool without,
        object? param,
        VectorResult vector,
        EvalNodeHelper enh)
    {
        if (op is AggregationOp.TopK or AggregationOp.BottomK)
        {
            if (param is not double k)
            {
                throw new InvalidOperationException($"{op} requires a scalar parameter");
            }

            if (k < 1)
            {
                return new VectorResult();
            }
        }

        if (op == AggregationOp.Quantile)
        {
            if (param is not double d)
            {
                throw new InvalidOperationException($"{op} requires a scalar parameter");
            }

            var q = d;
            if (q is < 0 or > 1)
            {
                throw new InvalidOperationException($"{op} requires a scalar parameter between 0 and 1");
            }
        }

        if (op == AggregationOp.CountValues)
        {
            if (param is not string s)
            {
                throw new InvalidOperationException($"{op} requires a string parameter");
            }

            var valueLabel = s;
            // TODO: validate that valueLabel is a valid label name
            if (!without)
            {
                grouping = [.. grouping, valueLabel];
            }
        }

        foreach (var groupedAggregation in vector
                     .GroupBy(s => s.Metric.MatchLabels(!without, grouping)))
        {
            var metric = new Labels();
            if (without)
            {
                foreach (var label in groupedAggregation.Key)
                {
                    if (!grouping.Contains(label.Key))
                    {
                        metric[label.Key] = label.Value;
                    }
                }
            }
            else
            {
                foreach (var labelName in grouping)
                {
                    if (groupedAggregation.Key.TryGetValue(labelName, out var labelValue))
                    {
                        metric[labelName] = labelValue;
                    }
                }
            }

            double? value = op switch
            {
                AggregationOp.Sum => groupedAggregation.Sum(x => x.Point.Value),
                AggregationOp.Min => groupedAggregation
                    .Select(x => x.Point.Value)
                    .Where(x => !double.IsNaN(x))
                    .Min(x => x),
                AggregationOp.Max => groupedAggregation.Max(x => x.Point.Value),
                AggregationOp.Avg => groupedAggregation.Average(x => x.Point.Value),
                AggregationOp.StdVar => groupedAggregation
                    .Select(x => x.Point.Value)
                    .Where(x => !double.IsNaN(x))
                    .StandardVariance(),
                AggregationOp.StdDev => groupedAggregation
                    .Select(x => x.Point.Value)
                    .Where(x => !double.IsNaN(x))
                    .StandardDeviation(),
                AggregationOp.Count => groupedAggregation.Count(),
                _ => null
            };
            if (value.HasValue)
            {
                enh.Output.Add(new Sample { Metric = metric, Point = new DoublePoint { Value = value.Value } });
                continue;
            }

            switch (op)
            {
                case AggregationOp.TopK:
                    {
                        var k = (int)(double)param!;
                        var topK = groupedAggregation
                            .OrderByDescending(x => x.Point.Value)
                            .Take(k);
                        enh.Output.AddRange(topK);
                        break;
                    }
                case AggregationOp.BottomK:
                    {
                        var k = (int)(double)param!;
                        var bottomK = groupedAggregation.OrderBy(x => x.Point.Value).Take(k);
                        enh.Output.AddRange(bottomK);
                        break;
                    }
                default:
                    throw new NotSupportedException($"Aggregation operation {op} is not supported.");
            }
        }

        return enh.Output;
    }

    private VectorResult VectorSelector(VectorSelector vectorSelector, long timeStampSeconds)
    {
        var result = new VectorResult(vectorSelector.Series.Count());
        var ts = timeStampSeconds - vectorSelector.Offset.TotalSeconds;

        foreach (var timeSeries in vectorSelector.Series)
        {
            foreach (var sample in timeSeries.Samples)
            {
                if (sample.TimestampUnixSec < ts)
                {
                    continue;
                }

                result.Add(new Sample
                {
                    Metric = new Labels(timeSeries.Labels),
                    Point = new DoublePoint { Value = sample.Value }
                });
                break;
            }
        }

        return result;
    }

    private static bool ShouldDropMetricName(Operator op) =>
        op switch
        {
            Operator.Add => true,
            Operator.Sub => true,
            Operator.Div => true,
            Operator.Mul => true,
            Operator.Pow => true,
            Operator.Mod => true,
            _ => false
        };
}
