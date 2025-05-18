// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Mocha.Core.Models.Metrics;
using Mocha.Core.Storage;
using Mocha.Storage.EntityFrameworkCore.Metadata.Models;

namespace Mocha.Storage.EntityFrameworkCore.Metadata.Writers;

public class EFPrometheusMetricMetadataWriter(IDbContextFactory<MochaMetadataContext> contextFactory)
    : ITelemetryDataWriter<MochaMetricMetadata>
{
    public async Task WriteAsync(IEnumerable<MochaMetricMetadata> data)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var newMetadataList = new List<EFMetricMetadata>();
        foreach (var metadata in data)
        {
            var existing = await context.MetricMetadata
                .FirstOrDefaultAsync(m => m.Metric == metadata.Metric && m.ServiceName == metadata.ServiceName);

            if (existing is not null)
            {
                existing.Type = metadata.Type;
                existing.Description = metadata.Description;
                existing.Unit = metadata.Unit;
            }
            else
            {
                newMetadataList.Add(new EFMetricMetadata
                {
                    Metric = metadata.Metric,
                    ServiceName = metadata.ServiceName,
                    Type = metadata.Type,
                    Description = metadata.Description,
                    Unit = metadata.Unit,
                });
            }
        }

        context.MetricMetadata.AddRange(newMetadataList);
        await context.SaveChangesAsync();
    }
}
