// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Core.Models.Metrics;
using Mocha.Core.Storage;
using Mocha.Core.Storage.Prometheus;
using Mocha.Storage.EntityFrameworkCore.Metadata.Readers;
using Mocha.Storage.EntityFrameworkCore.Metadata.Writers;

namespace Mocha.Storage.EntityFrameworkCore.Metadata;

public static class EFMetadataStorageOptionsBuilderExtensions
{
    public static MetadataStorageOptionsBuilder UseEntityFrameworkCore(
        this MetadataStorageOptionsBuilder builder,
        Action<DbContextOptionsBuilder> optionsAction)
    {
        builder.Services.AddSingleton<ITelemetryDataWriter<MochaMetricMetadata>, EFPrometheusMetricMetadataWriter>();
        builder.Services.AddSingleton<IPrometheusMetricMetadataReader, EFPrometheusMetricMetadataReader>();
        builder.Services.AddPooledDbContextFactory<MochaMetadataContext>(optionsAction);
        return builder;
    }
}
