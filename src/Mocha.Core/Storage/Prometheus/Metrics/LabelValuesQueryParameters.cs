// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Storage.Prometheus.Metrics;

public class LabelValuesQueryParameters
{
    public required string LabelName { get; set; }

    public required long? StartTimestampUnixSec { get; set; }

    public required long? EndTimestampUnixSec { get; set; }

    public int? Limit { get; set; }
}
