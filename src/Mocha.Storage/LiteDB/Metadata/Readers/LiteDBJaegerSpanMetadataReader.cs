// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Storage.Jaeger;
using Mocha.Storage.LiteDB.Metadata.Models;

namespace Mocha.Storage.LiteDB.Metadata.Readers;

internal class LiteDBJaegerSpanMetadataReader(ILiteDBCollectionAccessor<LiteDBSpanMetadata> collectionAccessor)
    : IJaegerSpanMetadataReader
{
    public Task<IEnumerable<string>> GetServicesAsync()
    {
        var services = collectionAccessor.Collection
            .Query()
            .Select(m => m.ServiceName)
            .ToEnumerable()
            .Distinct()
            .Order()
            .ToList();

        return Task.FromResult<IEnumerable<string>>(services);
    }

    public Task<IEnumerable<string>> GetOperationsAsync(string serviceName)
    {
        var operations = collectionAccessor.Collection
            .Query()
            .Where(m => m.ServiceName == serviceName)
            .Select(m => m.OperationName)
            .ToEnumerable()
            .Distinct()
            .Order()
            .ToList();

        return Task.FromResult<IEnumerable<string>>(operations);
    }
}
