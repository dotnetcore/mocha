// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Mocha.Core.Models.Metrics;
using Mocha.Query.Prometheus.PromQL.Engine;
using Mocha.Query.Prometheus.PromQL.Values;

namespace Mocha.Query.Tests.Prometheus.Engine.Functions;

public class AbsTests
{
    [Fact]
    public async Task Eval_Abs()
    {
        var series = new[]
        {
            GenerateTimeSeries(
                "http_requests{job=\"api-server\", instance=\"0\", group=\"production\"}",
                TimeSpan.FromMinutes(5),
                10,
                0,
                10),
            GenerateTimeSeries(
                "http_requests{job=\"api-server\", instance=\"1\", group=\"production\"}",
                TimeSpan.FromMinutes(5),
                10,
                0,
                20),
            GenerateTimeSeries(
                "http_requests{job=\"api-server\", instance=\"0\", group=\"canary\"}",
                TimeSpan.FromMinutes(5),
                10,
                0,
                30),
            GenerateTimeSeries(
                "http_requests{job=\"api-server\", instance=\"1\", group=\"canary\"}",
                TimeSpan.FromMinutes(5),
                10,
                0,
                40),
            GenerateTimeSeries(
                "http_requests{job=\"app-server\", instance=\"0\", group=\"production\"}",
                TimeSpan.FromMinutes(5),
                10,
                0,
                50),
            GenerateTimeSeries(
                "http_requests{job=\"app-server\", instance=\"1\", group=\"production\"}",
                TimeSpan.FromMinutes(5),
                10,
                0,
                60),
            GenerateTimeSeries(
                "http_requests{job=\"app-server\", instance=\"0\", group=\"canary\"}",
                TimeSpan.FromMinutes(5),
                10,
                0,
                70),
            GenerateTimeSeries(
                "http_requests{job=\"app-server\", instance=\"1\", group=\"canary\"}",
                TimeSpan.FromMinutes(5),
                10,
                0,
                80),
        };

        var engine = new PromQLEngine(
            new MochaPromQLParserParser(),
            new InMemoryPrometheusMetricsReader(series),
            Options.Create(new PromQLEngineOptions()));

        var result = await engine.QueryInstantAsync(
            "abs(-1 * http_requests{group=\"production\",job=\"api-server\"})",
            50 * 60,
            null,
            CancellationToken.None);

        result.Should().BeEquivalentTo(
            new VectorResult
            {
                new Sample
                {
                    Metric = new Labels
                    {
                        { "job", "api-server" }, { "instance", "0" }, { "group", "production" }
                    },
                    Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 100 }
                },
                new Sample
                {
                    Metric = new Labels
                    {
                        { "job", "api-server" }, { "instance", "1" }, { "group", "production" }
                    },
                    Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 200 }
                }
            },
            opts => opts.RespectingRuntimeTypes()
                .Using<double>(ctx =>
                    ctx.Subject.Should().BeApproximately(ctx.Expectation, 1e-12))
                .WhenTypeIs<double>());
    }
}
