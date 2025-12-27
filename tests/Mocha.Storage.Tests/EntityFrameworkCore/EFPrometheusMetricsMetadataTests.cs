// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Core.Models.Metadata;
using Mocha.Core.Models.Metrics;
using Mocha.Core.Storage;
using Mocha.Core.Storage.Prometheus;
using Mocha.Core.Storage.Prometheus.Metrics;
using Mocha.Storage.EntityFrameworkCore.Metadata;
using Mocha.Storage.EntityFrameworkCore.Metadata.Readers;

namespace Mocha.Storage.Tests.EntityFrameworkCore;

public class EFPrometheusMetricsMetadataTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ITelemetryDataWriter<MochaMetricMetadata> _writer;
    private readonly IPrometheusMetricsMetadataReader _reader;

    public EFPrometheusMetricsMetadataTests()
    {
        var services = new ServiceCollection();
        services.AddStorage()
            .WithMetadata(tracingOptions =>
            {
                tracingOptions.UseEntityFrameworkCore(efOptions =>
                {
                    efOptions.UseInMemoryDatabase(Guid.NewGuid().ToString());
                });
            });

        _serviceProvider = services.BuildServiceProvider();
        _writer = _serviceProvider.GetRequiredService<ITelemetryDataWriter<MochaMetricMetadata>>();
        _reader = _serviceProvider.GetRequiredService<IPrometheusMetricsMetadataReader>();
    }

    [Fact]
    public async Task GetMetadataAsync()
    {
        var metadata = new List<MochaMetricMetadata>
        {
            new()
            {
                Metric = "cpu_usage",
                ServiceName = "serviceA",
                Type = MochaMetricType.Gauge,
                Description = "CPU Usage",
                Unit = "percentage"
            },
            new()
            {
                Metric = "memory_usage",
                ServiceName = "serviceB",
                Type = MochaMetricType.Gauge,
                Description = "Memory Usage",
                Unit = "MB"
            }
        };

        await _writer.WriteAsync(metadata);

        var result = await _reader.GetMetadataAsync();

        result.Should().BeEquivalentTo(new Dictionary<string, List<PrometheusMetricMetadata>>
        {
            {
                "cpu_usage", [
                    new()
                    {
                        Metric = "cpu_usage",
                        Type = MochaMetricType.Gauge.ToPrometheusType(),
                        Help = "CPU Usage",
                        Unit = "percentage"
                    }
                ]
            },
            {
                "memory_usage", [
                    new()
                    {
                        Metric = "memory_usage",
                        Type = MochaMetricType.Gauge.ToPrometheusType(),
                        Help = "Memory Usage",
                        Unit = "MB"
                    }
                ]
            }
        });
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }
}
