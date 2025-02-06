// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Storage.Prometheus.Metrics;

public class TimeSeries
{
    public required Labels Labels { get; set; }

    /// <summary>
    /// The samples of the time series, sorted by timestamp.
    /// </summary>
    public required IEnumerable<TimeSeriesSample> Samples { get; set; }
}
