// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Mocha.Core.Models.Metrics;

namespace Mocha.Core.Storage.Prometheus.Metrics;

public class TimeSeries
{
    public TimeSeries()
    {
    }

    [SetsRequiredMembers]
    public TimeSeries(Labels labels, IEnumerable<TimeSeriesSample> samples)
    {
        Labels = labels;
        Samples = samples;
    }

    public required Labels Labels { get; set; }

    /// <summary>
    /// The samples of the time series, sorted by timestamp.
    /// </summary>
    public required IEnumerable<TimeSeriesSample> Samples { get; set; }
}
