// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Core.Models.Metrics;
using Mocha.Core.Storage;
using Mocha.Core.Storage.Prometheus;
using Mocha.E2E.Abstractions;
using Mocha.E2E.Component.Tests.Fixtures;
using Mocha.E2E.Component.Tests.Support;
using Mocha.Query.Prometheus.PromQL.Values;
using Mocha.Storage;
using Mocha.Storage.InfluxDB;
using Mocha.Storage.InfluxDB.Metrics;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using Xunit;

namespace Mocha.E2E.Component.Tests;

/// <summary>
/// Tier-2 component test: seeds a gauge into a REAL InfluxDB 2.7.7 container through the production
/// metrics writer (<c>InfluxDBMetricsWriter</c>), then runs a PromQL instant query through the same
/// Query Prometheus path the service uses — the production <c>InfluxDbPrometheusMetricsReader</c>
/// feeding the (internal) <c>PromQLEngine</c> — and asserts the returned result vector.
/// </summary>
[Collection(InfluxDbCollection.Name)]
public sealed class InfluxDbPromQlQueryTests(InfluxDbContainerFixture influx)
{
    [Fact]
    public async Task WriteGaugeThenInstantQuery_ReturnsNonEmptyVectorFromRealInfluxDb()
    {
        // Arrange: wire the real InfluxDB metrics storage the same way Mocha.Query.Program does,
        // pointed at the container.
        var services = new ServiceCollection();
        services.AddStorage()
            .WithMetrics(metrics =>
            {
                metrics.UseInfluxDB(options =>
                {
                    options.Url = influx.Url;
                    options.Token = InfluxDbContainerFixture.AdminToken;
                    options.Org = InfluxDbContainerFixture.Organization;
                    options.Bucket = InfluxDbContainerFixture.Bucket;
                });
            });

        await using var provider = services.BuildServiceProvider();
        var writer = provider.GetRequiredService<ITelemetryDataWriter<MochaMetric>>();
        var reader = provider.GetRequiredService<IPrometheusMetricsReader>();

        // Build a gauge with the shared factory, then convert to the storage model with the same
        // production OTLP->Mocha extension the distributor uses (no hand-built metric).
        const double expectedValue = 42;
        var factory = new TelemetryFactory(new TelemetrySpec
        {
            ServiceName = "component-influx",
            Metrics =
            [
                new MetricSpec
                {
                    Name = "component_influx_gauge",
                    Type = "gauge",
                    Unit = "1",
                    Value = expectedValue,
                    Attributes = new Dictionary<string, string> { ["e2e_case"] = "influx-promql" }
                }
            ]
        });

        var metricName = factory.MetricNames[0];
        var mochaMetrics = ToMochaMetrics(factory.BuildMetricsRequest()).ToList();
        mochaMetrics.Should().ContainSingle();
        mochaMetrics[0].Name.Should().Be(metricName);

        // Act: write, then query through the real reader + production engine, polling for the
        // eventually-visible write.
        await writer.WriteAsync(mochaMetrics);

        var engine = PromQLEngineBridge.Create(reader);
        var vector = await PollForVectorAsync(
            async ct =>
            {
                var result = await engine.QueryInstantAsync(
                    metricName,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    limit: null,
                    ct);
                return result as VectorResult;
            },
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        // Assert
        vector.Should().NotBeNull();
        var samples = vector!;
        samples.Should().NotBeEmpty("the gauge written to InfluxDB must be queryable via PromQL");
        samples.Should().Contain(sample =>
            sample.Metric.ContainsKey(Labels.MetricName) && sample.Metric[Labels.MetricName] == metricName);
        samples[0].Point.Value.Should().BeApproximately(expectedValue, 1e-9);
    }

    private static async Task<VectorResult?> PollForVectorAsync(
        Func<CancellationToken, Task<VectorResult?>> query,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        VectorResult? last = null;
        while (DateTimeOffset.UtcNow < deadline)
        {
            last = await query(cancellationToken);
            if (last is { Count: > 0 })
            {
                return last;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }

        return last;
    }

    private static IEnumerable<MochaMetric> ToMochaMetrics(ExportMetricsServiceRequest request)
    {
        foreach (var resourceMetrics in request.ResourceMetrics)
        {
            var resourceLabels = resourceMetrics.Resource.Attributes.ToMochaMetricLabels();
            foreach (var scopeMetrics in resourceMetrics.ScopeMetrics)
            {
                foreach (var metric in scopeMetrics.Metrics)
                {
                    foreach (var mochaMetric in metric.ToMochaMetric(resourceLabels))
                    {
                        yield return mochaMetric;
                    }
                }
            }
        }
    }
}
