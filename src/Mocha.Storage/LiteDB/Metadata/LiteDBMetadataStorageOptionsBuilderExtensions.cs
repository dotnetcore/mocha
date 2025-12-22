// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Core.Models.Metadata;
using Mocha.Core.Models.Metrics;
using Mocha.Core.Storage;
using Mocha.Core.Storage.Jaeger;
using Mocha.Core.Storage.Prometheus;
using Mocha.Storage.LiteDB.Metadata.Models;
using Mocha.Storage.LiteDB.Metadata.Readers;
using Mocha.Storage.LiteDB.Metadata.Writers;

namespace Mocha.Storage.LiteDB.Metadata;

public static class LiteDBMetadataStorageOptionsBuilderExtensions
{
    public static MetadataStorageOptionsBuilder UseLiteDB(
        this MetadataStorageOptionsBuilder builder,
        Action<LiteDBMetadataOptions> configure)
    {
        builder.Services.AddOptions();
        builder.Services.Configure(configure);

        builder.Services
            .AddSingleton<ILiteDBCollectionAccessor<LiteDBSpanMetadata>, LiteDBSpanMetadataCollectionAccessor>();
        builder.Services.AddSingleton<ITelemetryDataWriter<MochaSpanMetadata>, LiteDBJaegerSpanMetadataWriter>();
        builder.Services.AddSingleton<IJaegerSpanMetadataReader, LiteDBJaegerSpanMetadataReader>();

        builder.Services
            .AddSingleton<ILiteDBCollectionAccessor<LiteDBMetricMetadata>, LiteDBMetricMetadataCollectionAccessor>();
        builder.Services
            .AddSingleton<ITelemetryDataWriter<MochaMetricMetadata>, LiteDBPrometheusMetricMetadataWriter>();
        builder.Services.AddSingleton<IPrometheusMetricMetadataReader, LiteDBPrometheusMetricMetadataReader>();

        return builder;
    }
}
