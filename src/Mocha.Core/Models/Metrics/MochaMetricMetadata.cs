// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Models.Metrics;

public record MochaMetricMetadata
{
    public required string Metric { get; init; }

    public required string ServiceName { get; init; }

    public required MochaMetricType Type { get; init; }

    public required string Description { get; init; }

    public required string Unit { get; init; }
}
