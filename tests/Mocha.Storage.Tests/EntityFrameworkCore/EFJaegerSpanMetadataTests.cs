// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Core.Models.Metadata;
using Mocha.Core.Storage;
using Mocha.Core.Storage.Jaeger;
using Mocha.Storage.EntityFrameworkCore.Metadata;

namespace Mocha.Storage.Tests.EntityFrameworkCore;

public class EFJaegerSpanMetadataTests : IDisposable
{
    private readonly ITelemetryDataWriter<MochaSpanMetadata> _writer;
    private readonly IJaegerSpanMetadataReader _reader;
    private readonly ServiceProvider _serviceProvider;

    public EFJaegerSpanMetadataTests()
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
        _reader = _serviceProvider.GetRequiredService<IJaegerSpanMetadataReader>();
        _writer = _serviceProvider.GetRequiredService<ITelemetryDataWriter<MochaSpanMetadata>>();
    }

    [Fact]
    public async Task GetServicesAsync()
    {
        await _writer.WriteAsync([
            new MochaSpanMetadata
            {
                ServiceName = "ServiceName1",
                OperationName = "OperationName1"
            },
            new MochaSpanMetadata
            {
                ServiceName = "ServiceName2",
                OperationName = "OperationName2"
            }
        ]);

        var services = await _reader.GetServicesAsync();
        Assert.Equal(["ServiceName1", "ServiceName2"], services);
    }

    [Fact]
    public async Task GetOperationsAsync()
    {
        _writer.WriteAsync([
            new MochaSpanMetadata
            {
                ServiceName = "ServiceName1",
                OperationName = "SpanName1"
            },
            new MochaSpanMetadata
            {
                ServiceName = "ServiceName1",
                OperationName = "SpanName2"
            },
            new MochaSpanMetadata
            {
                ServiceName = "ServiceName2",
                OperationName = "SpanName3"
            }]);

        var operations = await _reader.GetOperationsAsync("ServiceName1");
        Assert.Equal(["SpanName1", "SpanName2"], operations);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }
}
