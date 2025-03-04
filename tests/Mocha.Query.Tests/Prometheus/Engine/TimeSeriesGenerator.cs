// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Storage.Prometheus;
using Mocha.Core.Storage.Prometheus.Metrics;

namespace Mocha.Query.Tests.Prometheus.Engine;

/// <summary>
/// Generates time series for testing.
/// </summary>
public static class TimeSeriesGenerator
{
    /// <summary>
    /// Generates a time series with the given labels, interval, count, start value and step.
    /// </summary>
    /// <param name="labels">The labels of the time series.</param>
    /// <param name="interval">The interval between samples.</param>
    /// <param name="iterations">
    /// The number of samples to generate, the first sample will not be counted.
    /// The first sample is at 0, and the last sample is at iterations * interval.
    /// </param>
    /// <param name="startValue">The value of the first sample.</param>
    /// <param name="step">The step between samples.</param>
    /// <returns>The generated time series.</returns>
    public static TimeSeries GenerateTimeSeries(
        Labels labels,
        TimeSpan interval,
        int iterations,
        double startValue,
        double step) =>
        GenerateTimeSeries(labels, 0, interval, iterations, startValue, step);

    /// <summary>
    /// Generates a time series with the given labels, interval, count, start value and step.
    /// </summary>
    /// <param name="labels">The labels of the time series.</param>
    /// <param name="startTimestampUnixSec">The timestamp of the first sample.</param>
    /// <param name="interval">The interval between samples.</param>
    /// <param name="iterations">
    /// The number of samples to generate, the first sample will not be counted.
    /// The first sample is at 0, and the last sample is at iterations * interval.
    /// </param>
    /// <param name="startValue">The value of the first sample.</param>
    /// <param name="step">The step between samples.</param>
    /// <returns>The generated time series.</returns>
    public static TimeSeries GenerateTimeSeries(
        Labels labels,
        int startTimestampUnixSec,
        TimeSpan interval,
        int iterations,
        double startValue,
        double step)
    {
        var samples = new List<TimeSeriesSample>();
        for (var i = 0; i <= iterations; i++)
        {
            samples.Add(new TimeSeriesSample
            {
                TimestampUnixSec = startTimestampUnixSec + (long)(i * interval.TotalSeconds),
                Value = startValue + i * step
            });
        }

        return new TimeSeries(labels, samples);
    }

    public static TimeSeries Merge(params TimeSeries[] timeSeries)
    {
        var merged = new TimeSeries(timeSeries[0].Labels, []);
        foreach (var series in timeSeries)
        {
            merged.Samples = merged.Samples.Concat(series.Samples);
        }

        return merged;
    }
}
