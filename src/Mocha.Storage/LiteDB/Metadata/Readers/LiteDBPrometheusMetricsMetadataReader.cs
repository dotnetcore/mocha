// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Storage.Prometheus;
using Mocha.Core.Storage.Prometheus.Metrics;
using Mocha.Storage.LiteDB.Metadata.Models;

namespace Mocha.Storage.LiteDB.Metadata.Readers;

internal class LiteDBPrometheusMetricsMetadataReader(
    ILiteDBCollectionAccessor<LiteDBMetricMetadata> collectionAccessor)
    : IPrometheusMetricsMetadataReader
{
    public Task<Dictionary<string, List<PrometheusMetricMetadata>>> GetMetadataAsync(
        string? metricName = null,
        int? limit = null)
    {
        var queryable = collectionAccessor.Collection.Query();

        if (!string.IsNullOrWhiteSpace(metricName))
        {
            queryable = queryable.Where(m => m.Metric == metricName);
        }

        var metadataList = limit.HasValue ? queryable.Limit(limit.Value).ToList() : queryable.ToList();

        var result = new Dictionary<string, List<PrometheusMetricMetadata>>();

        foreach (var metadata in metadataList)
        {
            if (!result.TryGetValue(metadata.Metric, out var list))
            {
                list = new List<PrometheusMetricMetadata>();
                result[metadata.Metric] = list;
            }

            list.Add(new PrometheusMetricMetadata
            {
                Metric = metadata.Metric,
                Type = metadata.Type.ToPrometheusType(),
                Help = metadata.Description,
                Unit = metadata.Unit
            });
        }

        return Task.FromResult(result);
    }

    public Task<IEnumerable<string>> GetMetricNamesAsync()
    {
        var metricNames = collectionAccessor.Collection.Query()
            .Select(m => m.Metric)
            .ToList()
            .ToHashSet();

        return Task.FromResult<IEnumerable<string>>(metricNames);
    }
}
