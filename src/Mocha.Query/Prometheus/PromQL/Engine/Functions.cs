// Copyright 2013 The Prometheus Authors
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Mocha.Core.Extensions;
using Mocha.Core.Models.Metrics;
using Mocha.Core.Storage.Prometheus;
using Mocha.Core.Storage.Prometheus.Metrics;
using Mocha.Query.Prometheus.PromQL.Ast;
using Mocha.Query.Prometheus.PromQL.Values;

namespace Mocha.Query.Prometheus.PromQL.Engine;

internal static class Functions
{
    #region Function Implementations

    // abs(v instant-vector)
    // returns the input vector with all sample values converted to their absolute value.
    internal static VectorResult FuncAbs(IParseResult[] values, Expression[] args, EvalNodeHelper enh) =>
        SimpleFunc(values, enh, Math.Abs);

    // absent(v instant-vector)
    // returns an empty vector if the vector passed to it has any elements (floats or native histograms)
    // and a 1-element vector with the value 1 if the vector passed to it has no elements.
    public static VectorResult FuncAbsent(IParseResult[] values, Expression[] args, EvalNodeHelper enh)
    {
        var vector = (VectorResult)values[0];
        // return empty vector if the input vector exists
        if (vector.Count > 0)
        {
            return enh.Output;
        }

        var labels = new Labels();
        if (args[0] is VectorSelector vectorSelector)
        {
            foreach (var matcher in vectorSelector.LabelMatchers)
            {
                if (matcher is { Type: LabelMatcherType.Equal, Name: not Labels.MetricName })
                {
                    labels.Add(matcher.Name, matcher.Value);
                }
            }
        }

        enh.Output.Add(new Sample { Metric = labels, Point = new DoublePoint { Value = 1 } });
        return enh.Output;
    }

    // ceil
    // changes
    // clamp_max
    // clamp_min

    // days_in_month
    // day_of_month
    // day_of_week
    // delta
    // deriv
    // exp
    // floor

    // histogram_quantile(φ scalar, b instant-vector)
    // calculates the φ-quantile (0 ≤ φ ≤ 1) from a classic histogram or from a native histogram.
    public static VectorResult FuncHistogramQuantile(IParseResult[] values, Expression[] args, EvalNodeHelper enh)
    {
        // TODO: why not use ((NumberLiteral)args[0]).Value
        var quantile = ((VectorResult)values[0])[0].Point.Value;
        var inputVector = (VectorResult)values[1];

        var metricsWithBuckets = inputVector
            .GroupBy(s =>
                s.Metric.MatchLabels(false, [Labels.MetricName, Labels.BucketLabel]));

        foreach (var mb in metricsWithBuckets)
        {
            if (!mb.Any())
            {
                continue;
            }

            var buckets = mb
                .Select(s =>
                {
                    var bucketValue = s.Metric[Labels.BucketLabel];
                    var upperBound = bucketValue == "+Inf" ? double.PositiveInfinity : double.Parse(bucketValue);

                    return new Bucket(upperBound, s.Point.Value);
                })
                .ToList();

            enh.Output.Add(new Sample
            {
                Metric = mb.Key.Drop(Labels.MetricName, Labels.BucketLabel),
                Point = new DoublePoint { Value = BucketQuantile(quantile, buckets) }
            });
        }

        return enh.Output;


        static double BucketQuantile(double quantile, List<Bucket> buckets)
        {
            if (quantile < 0)
            {
                return double.NegativeInfinity;
            }

            if (quantile > 1)
            {
                return double.PositiveInfinity;
            }

            buckets = [.. buckets.OrderBy(b => b.UpperBound)];
            if (!double.IsPositiveInfinity(buckets[^1].UpperBound))
            {
                return double.NaN;
            }

            buckets = CoalesceBuckets(buckets);
            EnsureMonotonic(buckets);

            if (buckets.Count < 2)
            {
                return double.NaN;
            }

            var rank = quantile * buckets[^1].Count;

            var b = 0;
            for (var i = 0; i < buckets.Count - 1; i++)
            {
                if (buckets[i].Count >= rank)
                {
                    b = i;
                    break;
                }
            }

            if (b == buckets.Count - 1)
            {
                return buckets[^2].UpperBound;
            }

            if (b == 0 && buckets[0].UpperBound <= 0)
            {
                return buckets[0].UpperBound;
            }

            var bucketStart = 0.0;

            var bucketEnd = buckets[b].UpperBound;
            var count = buckets[b].Count;

            if (b > 0)
            {
                bucketStart = buckets[b - 1].UpperBound;
                count -= buckets[b - 1].Count;
                rank -= buckets[b - 1].Count;
            }

            var bucketQuantile = bucketStart + (bucketEnd - bucketStart) * (rank / count);
            return bucketQuantile;
        }

        // The input buckets must be sorted.
        static List<Bucket> CoalesceBuckets(List<Bucket> buckets)
        {
            var last = buckets[0];
            var i = 0;
            foreach (var b in buckets[1..])
            {
                if (Math.Abs(b.UpperBound - last.UpperBound) <= 0)
                {
                    last.Count += b.Count;
                }
                else
                {
                    buckets[i] = last;
                    last = b;
                    i++;
                }
            }

            buckets[i] = last;
            return buckets[..(i + 1)];
        }

        static void EnsureMonotonic(List<Bucket> buckets)
        {
            var max = buckets[0].Count;
            for (var i = 1; i < buckets.Count; i++)
            {
                if (buckets[i].Count > max)
                {
                    max = buckets[i].Count;
                }
                else if (buckets[i].Count < max)
                {
                    buckets[i] = new Bucket(buckets[i].UpperBound, max);
                }
            }
        }
    }

    // holt_winters
    // hour
    // idelta

    // increase(v range-vector)
    // calculates the increase in the time series in the range vector.
    public static VectorResult FuncIncrease(IParseResult[] values, Expression[] args, EvalNodeHelper enh) =>
        ExtrapolatedRate(values, args, enh, true, false);

    // irate
    // label_replace
    // label_join
    // ln
    // log10
    // log2

    // minute
    // month
    // predict_linear
    // quantile_over_time

    // rate(v range-vector) calculates the per-second average rate of increase of the time series in the range vector.
    public static VectorResult FuncRate(IParseResult[] values, Expression[] args, EvalNodeHelper enh) =>
        ExtrapolatedRate(values, args, enh, true, true);
    // resets
    // round
    // scalar
    // sort
    // sort_desc
    // sqrt

    // time
    // timestamp
    // vector
    // year

    #region <aggregation>_over_time()

    // The following functions allow aggregating each series of a given range vector over time and return an instant vector with per-series aggregation results:

    // avg_over_time
    // the average value of all points in the specified interval.
    public static VectorResult FuncAvgOverTime(IParseResult[] values, Expression[] args, EvalNodeHelper enh) =>
        AggregateOverTime(values, enh, points => points.Average(v => v.Value));

    // min_over_time
    // the minimum value of all points in the specified interval.
    public static VectorResult FuncMinOverTime(IParseResult[] values, Expression[] args, EvalNodeHelper enh) =>
        AggregateOverTime(values, enh, points => points.Min(v => v.Value));

    // max_over_time
    // the maximum value of all points in the specified interval.
    public static VectorResult FuncMaxOverTime(IParseResult[] values, Expression[] args, EvalNodeHelper enh) =>
        AggregateOverTime(values, enh, points => points.Max(v => v.Value));

    // sum_over_time
    // the sum of all values in the specified interval.
    public static VectorResult FuncSumOverTime(IParseResult[] values, Expression[] args, EvalNodeHelper enh) =>
        AggregateOverTime(values, enh, points => points.Sum(v => v.Value));

    // count_over_time
    // the count of all values in the specified interval.
    public static VectorResult FuncCountOverTime(IParseResult[] values, Expression[] args, EvalNodeHelper enh) =>
        AggregateOverTime(values, enh, points => points.Count);

    // stdvar_over_time
    // the population standard variance of the values in the specified interval.
    public static VectorResult FuncStdVarOverTime(IParseResult[] values, Expression[] args, EvalNodeHelper enh) =>
        AggregateOverTime(values, enh, points =>
            points.Select(v => v.Value)
                .Where(v => !double.IsNaN(v))
                .StandardVariance());

    // stddev_over_time
    // the population standard deviation of the values in the specified interval.
    public static VectorResult FuncStdDevOverTime(IParseResult[] values, Expression[] args, EvalNodeHelper enh) =>
        AggregateOverTime(values, enh, points =>
            points.Select(v => v.Value)
                .Where(v => !double.IsNaN(v))
                .StandardDeviation());

    #endregion

    #endregion

    #region Private Methods

    private static VectorResult SimpleFunc(IParseResult[] values, EvalNodeHelper enh, Func<double, double> func)
    {
        foreach (var sample in (VectorResult)values[0])
        {
            enh.Output.Add(new Sample
            {
                Metric = sample.Metric.DropMetricName(),
                Point = new DoublePoint { Value = func(sample.Point.Value) }
            });
        }

        return enh.Output;
    }

    private static VectorResult ExtrapolatedRate(
        IParseResult[] values, Expression[] args, EvalNodeHelper enh, bool isCounter, bool isRate)
    {
        var matrixSelector = (MatrixSelector)args[0];

        var matrix = (MatrixResult)values[0];
        var rangeStartSec = enh.TimestampUnixSec - (matrixSelector.Range + matrixSelector.Offset).TotalSeconds;
        var rangeEndSec = enh.TimestampUnixSec - matrixSelector.Offset.TotalSeconds;

        foreach (var samples in matrix)
        {
            // No sense in trying to compute a rate without at least two points. Drop
            // this Vector element.
            if (samples.Points.Count < 2)
            {
                continue;
            }

            double counterCorrection = 0;
            double lastValue = 0;
            foreach (var sample in samples.Points)
            {
                if (isCounter && sample.Value < lastValue)
                {
                    counterCorrection += lastValue;
                }

                lastValue = sample.Value;
            }

            var resultValue = lastValue - samples.Points[0].Value + counterCorrection;

            // Duration between first/last samples and boundary of range.
            var durationToStartSec = samples.Points[0].TimestampUnixSec - rangeStartSec;
            var durationToEndSec = rangeEndSec - samples.Points[^1].TimestampUnixSec;
            var sampledIntervalSec =
                (double)(samples.Points[^1].TimestampUnixSec - samples.Points[0].TimestampUnixSec);
            var averageDurationBetweenSamples = sampledIntervalSec / (samples.Points.Count - 1);

            if (isCounter && resultValue > 0 && samples.Points[0].Value >= 0)
            {
                // Counters cannot be negative. If we have any slope at
                // all (i.e. resultValue went up), we can extrapolate
                // the zero point of the counter. If the duration to the
                // zero point is shorter than the durationToStart, we
                // take the zero point as the start of the series,
                // thereby avoiding extrapolation to negative counter
                // values.
                var durationToZero = sampledIntervalSec * (samples.Points[0].Value / resultValue);
                if (durationToZero < durationToStartSec)
                {
                    durationToStartSec = durationToZero;
                }
            }

            // If the first/last samples are close to the boundaries of the range,
            // extrapolate the result. This is as we expect that another sample
            // will exist given the spacing between samples we've seen thus far,
            // with an allowance for noise.
            var extrapolationThreshold = averageDurationBetweenSamples * 1.1;
            var extrapolateToIntervalSec = sampledIntervalSec;

            if (durationToStartSec < extrapolationThreshold)
            {
                extrapolateToIntervalSec += durationToStartSec;
            }
            else
            {
                extrapolateToIntervalSec += averageDurationBetweenSamples / 2;
            }

            if (durationToEndSec < extrapolationThreshold)
            {
                extrapolateToIntervalSec += durationToEndSec;
            }
            else
            {
                extrapolateToIntervalSec += averageDurationBetweenSamples / 2;
            }

            resultValue *= extrapolateToIntervalSec / sampledIntervalSec;

            if (isRate)
            {
                resultValue /= matrixSelector.Range.TotalSeconds;
            }

            enh.Output.Add(new Sample { Metric = Labels.Empty, Point = new DoublePoint { Value = resultValue } });
        }

        return enh.Output;
    }

    private static VectorResult AggregateOverTime(
        IParseResult[] values,
        EvalNodeHelper enh,
        Func<List<DoublePoint>,
            double> aggregate)
    {
        var matrix = (MatrixResult)values[0];

        foreach (var samples in matrix)
        {
            if (samples.Points.Count == 0)
            {
                continue;
            }

            enh.Output.Add(new Sample
            {
                Metric = Labels.Empty,
                Point = new DoublePoint { Value = aggregate(samples.Points) }
            });
        }

        return enh.Output;
    }

    #endregion

    private struct Bucket(double upperBound, double count)
    {
        public readonly double UpperBound = upperBound;
        public double Count = count;
    }
}
