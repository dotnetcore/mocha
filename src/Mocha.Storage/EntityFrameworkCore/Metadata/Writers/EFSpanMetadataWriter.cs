// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Mocha.Core.Models.Metadata;
using Mocha.Core.Storage;
using Mocha.Storage.EntityFrameworkCore.Metadata.Models;

namespace Mocha.Storage.EntityFrameworkCore.Metadata.Writers;

internal class EFSpanMetadataWriter(IDbContextFactory<MochaMetadataContext> contextFactory) : ITelemetryDataWriter<MochaSpanMetadata>
{
    public async Task WriteAsync(IEnumerable<MochaSpanMetadata> data)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var newMetadataList = new List<EFSpanMetadata>();
        foreach (var metadata in data)
        {
            var existing = await context.SpanMetadata
                .FirstOrDefaultAsync(m => m.ServiceName == metadata.ServiceName && m.OperationName == metadata.OperationName);

            if (existing is null)
            {
                newMetadataList.Add(new EFSpanMetadata
                {
                    ServiceName = metadata.ServiceName,
                    OperationName = metadata.OperationName,
                });
            }
        }

        context.SpanMetadata.AddRange(newMetadataList);
        await context.SaveChangesAsync();
    }
}
