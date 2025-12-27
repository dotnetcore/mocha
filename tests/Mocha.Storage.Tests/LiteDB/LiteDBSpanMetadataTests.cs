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
        var services = new ServiceCollection();
        services.AddStorage()
            .WithMetadata(metadataOptions =>
            {
                metadataOptions.UseLiteDB(liteDbOptions =>
                {
                    liteDbOptions.DatabasePath = ":memory:";
                });
            });
        _serviceProvider = services.BuildServiceProvider();
        _reader = _serviceProvider.GetRequiredService<IJaegerSpanMetadataReader>();
        _writer = _serviceProvider.GetRequiredService<ITelemetryDataWriter<MochaSpanMetadata>>();
    }

    public void Dispose()
    {
        _tempDatabasePath.Dispose();
        _serviceProvider.Dispose();
    }
}
