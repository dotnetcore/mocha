// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Mocha.Core.Storage.Prometheus;
using Mocha.Core.Storage.Prometheus.Metrics;

namespace Mocha.Storage.EntityFrameworkCore.Metadata.Readers;

internal class EFPrometheusMetricsMetadataReader(IDbContextFactory<MochaMetadataContext> contextFactory)
    : IPrometheusMetricsMetadataReader
{
    public async Task<Dictionary<string, List<PrometheusMetricMetadata>>> GetMetadataAsync(
        string? metricName = null, int? limit = null)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var query = context.MetricMetadata.AsQueryable();

        if (!string.IsNullOrWhiteSpace(metricName))
        {
            query = query.Where(m => m.Metric == metricName);
        }

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        var metadataList = await query.GroupBy(m => new { m.Metric, m.ServiceName })
            .Select(g => g.First())
            .ToListAsync();

        var result = new Dictionary<string, List<PrometheusMetricMetadata>>();

        foreach (var metadata in metadataList)
        {
            if (!result.TryGetValue(metadata.Metric, out var list))
            {
                list = [];
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

        return result;
    }

    public async Task<IEnumerable<string>> GetMetricNamesAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var metricNames = await context.MetricMetadata.Select(m => m.Metric).Distinct().ToListAsync();
        return metricNames;
    }
}
