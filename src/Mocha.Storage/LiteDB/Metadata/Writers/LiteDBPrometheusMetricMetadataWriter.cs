// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Models.Metadata;
using Mocha.Core.Storage;
using Mocha.Storage.LiteDB.Metadata.Models;

namespace Mocha.Storage.LiteDB.Metadata.Writers;

internal class LiteDBPrometheusMetricMetadataWriter(
    ILiteDBCollectionAccessor<LiteDBMetricMetadata> collectionAccessor)
    : ITelemetryDataWriter<MochaMetricMetadata>
{
    public Task WriteAsync(IEnumerable<MochaMetricMetadata> data)
    {
        var collection = collectionAccessor.Collection;

        foreach (var metadata in data)
        {
            var existing =
                collection.FindOne(m => m.Metric == metadata.Metric && m.ServiceName == metadata.ServiceName);

            if (existing is not null)
            {
                existing.Type = metadata.Type;
                existing.Description = metadata.Description;
                existing.Unit = metadata.Unit;

                collection.Update(existing);
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

                collection.Insert(newMetadata);
            }
        }

        return Task.CompletedTask;
    }
}
