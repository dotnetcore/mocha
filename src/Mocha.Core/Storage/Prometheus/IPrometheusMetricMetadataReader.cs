// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Storage.Prometheus.Metrics;

namespace Mocha.Core.Storage.Prometheus;

public interface IPrometheusMetricMetadataReader
{
    Task<Dictionary<string, List<PrometheusMetricMetadata>>> GetMetadataAsync(string? metricName = null,
        int? limit = null);

    Task<IEnumerable<string>> GetMetricNamesAsync();
}
