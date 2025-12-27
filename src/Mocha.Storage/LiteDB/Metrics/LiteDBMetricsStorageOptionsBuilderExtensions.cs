// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Mocha.Core.Models.Metrics;
using Mocha.Core.Storage;
using Mocha.Core.Storage.Prometheus;
using Mocha.Storage.LiteDB.Metrics.Models;
using Mocha.Storage.LiteDB.Metrics.Readers.Prometheus;
using Mocha.Storage.LiteDB.Metrics.Writers;

namespace Mocha.Storage.LiteDB.Metrics;

public static class LiteDBMetricsStorageOptionsBuilderExtensions
{
    public static MetricsStorageOptionsBuilder UseLiteDB(
        this MetricsStorageOptionsBuilder builder,
        Action<LiteDBMetricsOptions> configure)
    {
        builder.Services.AddOptions();
        builder.Services.Configure(configure);

        builder.Services.AddSingleton<ILiteDBCollectionAccessor<LiteDBMetric>, LiteDBMetricsCollectionAccessor>();
        builder.Services.AddSingleton<ITelemetryDataWriter<MochaMetric>, LiteDBMetricsWriter>();
        builder.Services.AddSingleton<IPrometheusMetricsReader, LiteDBPrometheusMetricsReader>();

        return builder;
    }
}
