// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Mocha.Core.Storage.Prometheus;
using Mocha.Core.Storage.Prometheus.Metrics;
using Mocha.Query.Prometheus.PromQL.Ast;
using Mocha.Query.Prometheus.PromQL.Values;

namespace Mocha.Query.Prometheus.PromQL.Engine;

internal class PromQLEngine(
    IPromQLParser promQLParser,
    IPrometheusMetricReader metricReader,
    IOptions<PromQLEngineOptions> optionsAccessor)
    : IPromQLEngine
{
    private readonly PromQLEngineOptions _options = optionsAccessor.Value;

    public async Task<IParseResult> QueryRangeAsync(
        string query,
        long startTimestampUnixSec,
        long endTimestampUnixSec,
        TimeSpan? step,
        CancellationToken cancellationToken)
    {
        // TODO: Can we remove the EvalStatement?
        var evalStatement = new EvalStatement
        {
            Expression = promQLParser.ParseExpression(query),
            StartTimestampUnixSec = startTimestampUnixSec,
            EndTimestampUnixSec = endTimestampUnixSec,
            Interval = step > TimeSpan.Zero ? step.Value : _options.DefaultEvaluationInterval,
        };

        await PopulateSeriesAsync(evalStatement, cancellationToken);

        var evaluator = new Evaluator
        {
            StartTimestampUnixSec = evalStatement.StartTimestampUnixSec,
            EndTimestampUnixSec = evalStatement.EndTimestampUnixSec,
            Interval = evalStatement.Interval,
            MaxSamples = _options.MaxSamplesPerQuery,
            DefaultEvalInterval = _options.DefaultEvaluationInterval,
        };

        var result = evaluator.Eval(evalStatement.Expression);

        return result;
    }

    public async Task<IParseResult> QueryInstantAsync(
        string query,
        long timestampUnixSec,
        CancellationToken cancellationToken)
    {
        var evalStatement = new EvalStatement
        {
            Expression = promQLParser.ParseExpression(query),
            StartTimestampUnixSec = timestampUnixSec,
            EndTimestampUnixSec = timestampUnixSec,
            Interval = TimeSpan.FromSeconds(1),
        };

        await PopulateSeriesAsync(evalStatement, cancellationToken);

        var evaluator = new Evaluator
        {
            StartTimestampUnixSec = evalStatement.StartTimestampUnixSec,
            EndTimestampUnixSec = evalStatement.EndTimestampUnixSec,
            Interval = TimeSpan.FromSeconds(1),
            MaxSamples = 1000_000, // TODO: Make this configurable
            DefaultEvalInterval = TimeSpan.FromSeconds(15), // TODO: Make this configurable
        };

        var result = evaluator.Eval(evalStatement.Expression);

        if (result is not MatrixResult matrixResult)
        {
            throw new InvalidOperationException("Expected a matrix result.");
        }

        switch (evalStatement.Expression.Type)
        {
            case PrometheusValueType.Vector:
                // Convert matrix with one value per series into vector.
                var vector = new VectorResult();
                vector.AddRange(matrixResult.Select(s => new Sample
                {
                    Metric = s.Metric,
                    // Point might have a different timestamp, force it to the evaluation
                    // timestamp as that is when we ran the evaluation.
                    Point = new DoublePoint { Value = s.Points[0].Value, TimestampUnixSeconds = timestampUnixSec }
                }));
                return vector;
            case PrometheusValueType.Scalar:
                return new ScalarResult
                {
                    Value = matrixResult[0].Points[0].Value,
                    TimestampUnixSec = timestampUnixSec
                };
            case PrometheusValueType.Matrix:
                return matrixResult;
            default:
                throw new InvalidOperationException($"Unexpected expression type: {evalStatement.Expression.Type}");
        }
    }


    // TODO: a pushdown can be applied to the function calls
    private async Task PopulateSeriesAsync(EvalStatement evalStatement, CancellationToken cancellationToken)
    {
        foreach (var node in evalStatement.Expression.Inspect())
        {
            TimeSeriesQueryParameters parameters;
            IEnumerable<TimeSeries> series;
            switch (node)
            {
                case VectorSelector vectorSelector:
                    parameters = new TimeSeriesQueryParameters
                    {
                        LabelMatchers = vectorSelector.LabelMatchers,
                        StartTimestampUnixSec =
                        evalStatement.StartTimestampUnixSec - (long)_options.LookBackDelta.TotalSeconds,
                        EndTimestampUnixSec = evalStatement.EndTimestampUnixSec,
                        Limit = _options.MaxSamplesPerQuery,
                    };
                    if (vectorSelector.Offset > TimeSpan.Zero)
                    {
                        parameters.StartTimestampUnixSec -= (long)vectorSelector.Offset.TotalSeconds;
                        parameters.EndTimestampUnixSec -= (long)vectorSelector.Offset.TotalSeconds;
                    }

                    series = await metricReader.GetTimeSeriesAsync(parameters);
                    vectorSelector.Series = series;
                    break;

                case MatrixSelector matrixSelector:
                    parameters = new TimeSeriesQueryParameters
                    {
                        LabelMatchers = matrixSelector.LabelMatchers,
                        StartTimestampUnixSec =
                            evalStatement.StartTimestampUnixSec - (long)_options.LookBackDelta.TotalSeconds,
                        EndTimestampUnixSec = evalStatement.EndTimestampUnixSec,
                        Limit = _options.MaxSamplesPerQuery,
                    };
                    if (matrixSelector.Offset > TimeSpan.Zero)
                    {
                        parameters.StartTimestampUnixSec -= (long)matrixSelector.Offset.TotalSeconds;
                        parameters.EndTimestampUnixSec -= (long)matrixSelector.Offset.TotalSeconds;
                    }

                    series = await metricReader.GetTimeSeriesAsync(parameters);
                    matrixSelector.Series = series;
                    break;
            }
        }
    }
}
