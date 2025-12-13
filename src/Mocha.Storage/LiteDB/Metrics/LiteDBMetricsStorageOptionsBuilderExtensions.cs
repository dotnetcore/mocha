// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Mocha.Core.Models.Metrics;
using Mocha.Core.Storage;
using Mocha.Core.Storage.Prometheus;
using Mocha.Storage.LiteDB.Metadata;
using Mocha.Storage.LiteDB.Metadata.Writers;
using Mocha.Storage.LiteDB.Metrics.Readers.Prometheus;
using Mocha.Storage.LiteDB.Metrics.Writers;

namespace Mocha.Storage.LiteDB.Metrics;

public static class LiteDBMetricsStorageOptionsBuilderExtensions
{
    public static MetricsStorageOptionsBuilder UseLiteDB(
        this MetricsStorageOptionsBuilder builder,
        Action<LiteDBMetricsOptions> optionsAction)
    {
        builder.Services.AddSingleton<ITelemetryDataWriter<MochaMetric>, LiteDBMetricWriter>();
        builder.Services.AddSingleton<IPrometheusMetricReader, LiteDBPrometheusMetricReader>();
        builder.Services.Configure(optionsAction);

        return builder;
    }
}
