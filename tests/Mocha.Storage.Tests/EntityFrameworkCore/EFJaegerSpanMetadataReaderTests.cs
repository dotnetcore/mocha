// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Core.Storage.Jaeger;
using Mocha.Storage.EntityFrameworkCore.Metadata;
using Mocha.Storage.EntityFrameworkCore.Metadata.Models;

namespace Mocha.Storage.Tests.EntityFrameworkCore;

public class EFJaegerSpanMetadataReaderTests : IDisposable
{
    private readonly IDbContextFactory<MochaMetadataContext> _dbContextFactory;
    private readonly IJaegerSpanMetadataReader _jaegerSpanMetadataReader;
    private readonly ServiceProvider _serviceProvider;

    public EFJaegerSpanMetadataReaderTests()
    {
        var services = new ServiceCollection();
        services.AddStorage()
            .WithMetadata(metadataOptions =>
            {
                metadataOptions.UseEntityFrameworkCore(efOptions =>
                {
                    efOptions.UseInMemoryDatabase(Guid.NewGuid().ToString());
                });
            });
        _serviceProvider = services.BuildServiceProvider();
        _jaegerSpanMetadataReader = _serviceProvider.GetRequiredService<IJaegerSpanMetadataReader>();
        _dbContextFactory = _serviceProvider.GetRequiredService<IDbContextFactory<MochaMetadataContext>>();
    }

    [Fact]
    public async Task GetServicesAsync()
    {
        var spanMetadata = new[]
        {
            new EFSpanMetadata { ServiceName = "ServiceName1", OperationName = "OperationName1" },
            new EFSpanMetadata { ServiceName = "ServiceName2", OperationName = "OperationName2" }
        };

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.SpanMetadata.AddRangeAsync(spanMetadata);
        await context.SaveChangesAsync();

        var services = await _jaegerSpanMetadataReader.GetServicesAsync();
        Assert.Equal(["ServiceName1", "ServiceName2"], services);
    }

    [Fact]
    public async Task GetOperationsAsync()
    {
        var spanMetadata = new[]
        {
            new EFSpanMetadata { ServiceName = "ServiceName1", OperationName = "SpanName1" },
            new EFSpanMetadata { ServiceName = "ServiceName1", OperationName = "SpanName2" },
            new EFSpanMetadata { ServiceName = "ServiceName2", OperationName = "SpanName3" }
        };

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.SpanMetadata.AddRangeAsync(spanMetadata);
        await context.SaveChangesAsync();

        var operations = await _jaegerSpanMetadataReader.GetOperationsAsync("ServiceName1");
        Assert.Equal(["SpanName1", "SpanName2"], operations);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }
}
