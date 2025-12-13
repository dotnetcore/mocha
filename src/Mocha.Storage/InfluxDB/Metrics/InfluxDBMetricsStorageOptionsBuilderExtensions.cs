// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using InfluxDB.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mocha.Core.Models.Metrics;
using Mocha.Core.Storage;
using Mocha.Core.Storage.Prometheus;
using Mocha.Storage.InfluxDB.Metrics.Readers.Prometheus;
using Mocha.Storage.InfluxDB.Metrics.Writers;

namespace Mocha.Storage.InfluxDB.Metrics;

public static class InfluxDBMetricsStorageOptionsBuilderExtensions
{
    public static MetricsStorageOptionsBuilder UseInfluxDB(
        this MetricsStorageOptionsBuilder builder,
        Action<InfluxDBOptions> configure)
    {
        builder.Services.AddOptions();
        builder.Services.Configure(configure);
        builder.Services.AddSingleton<IInfluxDBClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<InfluxDBOptions>>().Value;
            return new InfluxDBClient(options.Url, options.Token);
        });
        builder.Services.AddSingleton<ITelemetryDataWriter<MochaMetric>, InfluxDBMetricWriter>();
        builder.Services.AddSingleton<IPrometheusMetricReader, InfluxDbPrometheusMetricReader>();
        return builder;
    }
}
