// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Storage.Prometheus.Metrics;

public class PrometheusMetricMetadata
{
    public required string Metric { get; set; }
    public required string Type { get; set; }
    public required string Help { get; set; }
    public required string Unit { get; set; }
}
