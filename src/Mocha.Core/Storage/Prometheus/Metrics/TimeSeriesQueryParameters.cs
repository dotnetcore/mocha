// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Storage.Prometheus.Metrics;

public class TimeSeriesQueryParameters
{
    public required IEnumerable<LabelMatcher> LabelMatchers { get; set; }

    public required long StartTimestampUnixSec { get; set; }

    public required long EndTimestampUnixSec { get; set; }

    public required int Limit { get; set; }

    // TODO: reduce returned data points by aggregating them according to Interval
    public required TimeSpan Interval { get; set; }

    // TODO: add hints for the query
}
