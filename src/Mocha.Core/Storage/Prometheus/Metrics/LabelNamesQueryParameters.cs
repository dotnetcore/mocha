// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Storage.Prometheus.Metrics;

public class LabelNamesQueryParameters
{
    public required IEnumerable<LabelMatcher> LabelMatchers { get; set; }

    public long? StartTimestampUnixSec { get; set; }

    public long? EndTimestampUnixSec { get; set; }
}
