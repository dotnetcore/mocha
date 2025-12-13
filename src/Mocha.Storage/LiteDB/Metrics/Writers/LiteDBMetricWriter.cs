// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using LiteDB;
using Microsoft.Extensions.Options;
using Mocha.Core.Models.Metrics;
using Mocha.Core.Storage;
using Mocha.Storage.LiteDB.Metrics.Models;

namespace Mocha.Storage.LiteDB.Metrics.Writers;

public class LiteDBMetricWriter : ITelemetryDataWriter<MochaMetric>, IDisposable
{
    private readonly ILiteDatabase _db;
    private readonly ILiteCollection<LiteDBMetric> _collection;

    public LiteDBMetricWriter(IOptions<LiteDBMetricsOptions> optionsAccessor)
    {
        var options = optionsAccessor.Value;
        var dbPath = Path.Combine(options.DatabasePath, LiteDBConstants.MetricsDatabaseFileName);
        _db = LiteDBUtils.OpenDatabase(dbPath);
        _collection = _db.GetCollection<LiteDBMetric>(LiteDBConstants.MetricsCollectionName);
    }


    public Task WriteAsync(IEnumerable<MochaMetric> data)
    {
        var liteDBMetrics = data.Select(metric => new LiteDBMetric
        {
            Name = metric.Name,
            Type = metric.Type,
            Unit = metric.Unit,
            Labels = metric.Labels.Select(l => $"{l.Key}={l.Value}").ToArray(),
            LabelNames = metric.Labels.Keys.ToArray(),
            Value = metric.Value,
            TimestampUnixNano = (long)metric.TimestampUnixNano
        });

        _collection.InsertBulk(liteDBMetrics);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
