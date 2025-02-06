// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Models.Metrics;

public class MochaMetric
{
    public required string Name { get; init; }

    public MochaMetricType Type { get; set; }

    public required string Description { get; init; }

    public required string Unit { get; init; }

    public required Dictionary<string, string> Labels { get; init; }

    public required double Value { get; init; }

    public required ulong TimestampUnixNano { get; init; }
}
