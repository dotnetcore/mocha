// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using LiteDB;
using Microsoft.Extensions.Options;
using Mocha.Core.Storage.Prometheus;
using Mocha.Core.Storage.Prometheus.Metrics;
using Mocha.Storage.LiteDB.Metadata.Models;

namespace Mocha.Storage.LiteDB.Metadata.Readers;

public class LiteDBPrometheusMetricMetadataReader : IPrometheusMetricMetadataReader, IDisposable
{
    private readonly ILiteDatabase _db;
    private readonly ILiteCollection<LiteDBMetricMetadata> _collection;

    public LiteDBPrometheusMetricMetadataReader(IOptions<LiteDBMetadataOptions> optionsAccessor)
    {
        var options = optionsAccessor.Value;
        var dbPath = Path.Combine(options.DatabasePath, LiteDBConstants.MetadataDatabaseFileName);
        _db = LiteDBUtils.OpenDatabase(dbPath);
        _collection = _db.GetCollection<LiteDBMetricMetadata>(LiteDBConstants.MetricsMetadataCollectionName);

        BsonMapper.Global.Entity<LiteDBMetricMetadata>().Id(x => x.Id);
        _collection.EnsureIndex(x => x.Metric);
        _collection.EnsureIndex(x => x.ServiceName);
    }

    public Task<Dictionary<string, List<PrometheusMetricMetadata>>> GetMetadataAsync(
        string? metricName = null,
        int? limit = null)
    {
        var queryable = _collection.Query();

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
        var metricNames = _collection.Query()
            .Select(m => m.Metric)
            .ToList()
            .ToHashSet();

        return Task.FromResult<IEnumerable<string>>(metricNames);
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
