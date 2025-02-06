// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Models.Metrics;

namespace Mocha.Storage.EntityFrameworkCore.Metadata.Models;

public class EFMetricMetadata
{
    public required string Metric { get; init; }

    public required string ServiceName { get; init; }

    public required MochaMetricType Type { get; set; }

    public required string Description { get; set; }

    public required string Unit { get; set; }
}
