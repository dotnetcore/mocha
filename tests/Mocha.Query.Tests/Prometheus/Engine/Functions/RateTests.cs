// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Mocha.Core.Models.Metrics;
using Mocha.Query.Prometheus.PromQL.Engine;
using Mocha.Query.Prometheus.PromQL.Values;

namespace Mocha.Query.Tests.Prometheus.Engine.Functions;

public class RateTests
{
    [Fact]
    public async Task Eval_Rate()
    {
        var series = new[]
        {
            GenerateTimeSeries(
                "http_requests{job=\"app-server\", instance=\"1\", group=\"canary\"}",
                TimeSpan.FromMinutes(5),
                10,
                0,
                80)
        };

        var engine = new PromQLEngine(
            new MochaPromQLParserParser(),
            new InMemoryPrometheusMetricsReader(series),
            Options.Create(new PromQLEngineOptions()));

        var result = await engine.QueryInstantAsync(
            "rate(http_requests{group=\"canary\", instance=\"1\", job=\"app-server\"}[50m])",
            50 * 60,
            null,
            CancellationToken.None);

        result.Should().BeEquivalentTo(
            new VectorResult
            {
                new Sample
                {
                    Metric = new Labels { { "job", "app-server" }, { "instance", "1" }, { "group", "canary" } },
                    Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 0.26666666666666666 }
                }
            },
            opts => opts.RespectingRuntimeTypes()
                .Using<double>(ctx =>
                    ctx.Subject.Should().BeApproximately(ctx.Expectation, 1e-12))
                .WhenTypeIs<double>());
    }
}
