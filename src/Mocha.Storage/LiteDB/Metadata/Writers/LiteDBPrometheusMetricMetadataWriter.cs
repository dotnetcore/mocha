// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using LiteDB;
using Microsoft.Extensions.Options;
using Mocha.Core.Models.Metrics;
using Mocha.Core.Storage;
using Mocha.Storage.LiteDB.Metadata.Models;

namespace Mocha.Storage.LiteDB.Metadata.Writers;

public class LiteDBPrometheusMetricMetadataWriter : ITelemetryDataWriter<MochaMetricMetadata>, IDisposable
{
    private readonly ILiteDatabase _db;
    private readonly ILiteCollection<LiteDBMetricMetadata> _collection;

    public LiteDBPrometheusMetricMetadataWriter(IOptions<LiteDBMetadataOptions> optionsAccessor)
    {
        var options = optionsAccessor.Value;
        var dbPath = Path.Combine(options.DatabasePath, LiteDBConstants.MetadataDatabaseFileName);
        _db = LiteDBUtils.OpenDatabase(dbPath);
        _collection = _db.GetCollection<LiteDBMetricMetadata>(LiteDBConstants.MetricsMetadataCollectionName);
    }

    public Task WriteAsync(IEnumerable<MochaMetricMetadata> data)
    {
        foreach (var metadata in data)
        {
            var existing = _collection.FindOne(m => m.Metric == metadata.Metric && m.ServiceName == metadata.ServiceName);

            if (existing is not null)
            {
                existing.Type = metadata.Type;
                existing.Description = metadata.Description;
                existing.Unit = metadata.Unit;

                _collection.Update(existing);
            }
            else
            {
                var newMetadata = new LiteDBMetricMetadata
                {
                    Metric = metadata.Metric,
                    ServiceName = metadata.ServiceName,
                    Type = metadata.Type,
                    Description = metadata.Description,
                    Unit = metadata.Unit,
                };

                _collection.Insert(newMetadata);
            }
        }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
