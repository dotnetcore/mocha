// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Mocha.Core.Models.Metadata;
using Mocha.Core.Storage;
using Mocha.Storage.LiteDB.Metadata.Models;

namespace Mocha.Storage.LiteDB.Metadata.Writers;

internal class LiteDBJaegerSpanMetadataWriter(ILiteDBCollectionAccessor<LiteDBSpanMetadata> collectionAccessor)
    : ITelemetryDataWriter<MochaSpanMetadata>
{
    public Task WriteAsync(IEnumerable<MochaSpanMetadata> data)
    {
        foreach (var metadata in data)
        {
            var collection = collectionAccessor.Collection;
            var existing = collection.FindOne(m =>
                m.ServiceName == metadata.ServiceName && m.OperationName == metadata.OperationName);

            if (existing is null)
            {
                var newMetadata = new LiteDBSpanMetadata
                {
                    ServiceName = metadata.ServiceName,
                    OperationName = metadata.OperationName
                };

                collection.Insert(newMetadata);
            }
        }

        return Task.CompletedTask;
    }
}
