// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Mocha.Core.Storage.Jaeger;

namespace Mocha.Storage.EntityFrameworkCore.Metadata.Readers;

internal class EFJaegerSpanMetadataReader(IDbContextFactory<MochaMetadataContext> contextFactory) : IJaegerSpanMetadataReader
{
    public async Task<IEnumerable<string>> GetServicesAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var services = await context.SpanMetadata
            .Select(sm => sm.ServiceName)
            .Distinct()
            .ToListAsync();
        return services;
    }

    public async Task<IEnumerable<string>> GetOperationsAsync(string serviceName)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var operations = await context.SpanMetadata
            .Where(sm => sm.ServiceName == serviceName)
            .Select(sm => sm.OperationName)
            .Distinct()
            .ToListAsync();
        return operations;
    }
}
