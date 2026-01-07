// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Mocha.Core.Models.Metadata;
using Mocha.Core.Storage;
using Mocha.Core.Storage.Jaeger;
using Mocha.Storage.LiteDB.Metadata;

namespace Mocha.Storage.Tests.LiteDB;

public class LiteDBSpanMetadataTests : IDisposable
{
    private readonly TempDatabasePath _tempDatabasePath;
    private readonly ServiceProvider _serviceProvider;

    private readonly ITelemetryDataWriter<MochaSpanMetadata> _writer;
    private readonly IJaegerSpanMetadataReader _reader;

    public LiteDBSpanMetadataTests()
    {
        _tempDatabasePath = TempDatabasePath.Create();
        var services = new ServiceCollection();
        services.AddStorage()
            .WithMetadata(metadataOptions =>
            {
                metadataOptions.UseLiteDB(liteDbOptions =>
                {
                    liteDbOptions.DatabasePath = _tempDatabasePath.Path;
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
        await _writer.WriteAsync([
            new MochaSpanMetadata
            {
                ServiceName = "ServiceName1",
                OperationName = "OperationName1"
            },
            new MochaSpanMetadata
            {
                ServiceName = "ServiceName1",
                OperationName = "OperationName2"
            }
        ]);

        var operations = await _reader.GetOperationsAsync("ServiceName1");
        Assert.Equal(["OperationName1", "OperationName2"], operations);
    }

    public void Dispose()
    {
        _tempDatabasePath.Dispose();
        _serviceProvider.Dispose();
    }
}
