// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Mocha.Core.Storage.Prometheus;
using Mocha.Query.Prometheus.PromQL.Engine;
using Mocha.Query.Prometheus.PromQL.Values;

namespace Mocha.Query.Tests.Prometheus.Engine;

public class OperatorTests
{
    [Theory]
    [MemberData(nameof(TestCases))]
    public async Task Eval_Instant_Operator(EngineTestCase testCase)
    {
        var series = new[]
        {
            GenerateTimeSeries(
                new Labels
                {
                    { Labels.MetricName, "http_requests" },
                    { "job", "api-server" },
                    { "instance", "0" },
                    { "group", "production" }
                },
                TimeSpan.FromMinutes(5), 10, 0, 10),
            GenerateTimeSeries(
                new Labels
                {
                    { Labels.MetricName, "http_requests" },
                    { "job", "api-server" },
                    { "instance", "1" },
                    { "group", "production" }
                },
                TimeSpan.FromMinutes(5), 10, 0, 20),
            GenerateTimeSeries(
                new Labels
                {
                    { Labels.MetricName, "http_requests" },
                    { "job", "api-server" },
                    { "instance", "0" },
                    { "group", "canary" }
                },
                TimeSpan.FromMinutes(5), 10, 0, 30),
            GenerateTimeSeries(
                new Labels
                {
                    { Labels.MetricName, "http_requests" },
                    { "job", "api-server" },
                    { "instance", "1" },
                    { "group", "canary" }
                },
                TimeSpan.FromMinutes(5), 10, 0, 40),
            GenerateTimeSeries(
                new Labels
                {
                    { Labels.MetricName, "http_requests" },
                    { "job", "app-server" },
                    { "instance", "0" },
                    { "group", "production" }
                },
                TimeSpan.FromMinutes(5), 10, 0, 50),
            GenerateTimeSeries(
                new Labels
                {
                    { Labels.MetricName, "http_requests" },
                    { "job", "app-server" },
                    { "instance", "1" },
                    { "group", "production" }
                },
                TimeSpan.FromMinutes(5), 10, 0, 60),
            GenerateTimeSeries(
                new Labels
                {
                    { Labels.MetricName, "http_requests" },
                    { "job", "app-server" },
                    { "instance", "0" },
                    { "group", "canary" }
                },
                TimeSpan.FromMinutes(5), 10, 0, 70),
            GenerateTimeSeries(
                new Labels
                {
                    { Labels.MetricName, "http_requests" },
                    { "job", "app-server" },
                    { "instance", "1" },
                    { "group", "canary" }
                },
                TimeSpan.FromMinutes(5), 10, 0, 80),
            GenerateTimeSeries(new Labels { { Labels.MetricName, "vector_matching_a" }, { "l", "x" } },
                TimeSpan.FromMinutes(1), 100, 0, 1),
            GenerateTimeSeries(new Labels { { Labels.MetricName, "vector_matching_a" }, { "l", "y" } },
                TimeSpan.FromMinutes(1), 50, 0, 2),
            GenerateTimeSeries(new Labels { { Labels.MetricName, "vector_matching_b" }, { "l", "x" } },
                TimeSpan.FromMinutes(1), 25, 0, 4),
        };

        var mockOptions = new Mock<IOptions<PromQLEngineOptions>>();
        mockOptions.SetupGet(x => x.Value).Returns(new PromQLEngineOptions
        {
            DefaultEvaluationInterval = TimeSpan.FromSeconds(15),
            MaxSamplesPerQuery = 50000000
        });

        var engine = new PromQLEngine(new Parser(), new InMemoryPrometheusMetricReader(series), mockOptions.Object);

        var result =
            await engine.QueryInstantAsync(testCase.Query, testCase.StartTimestampUnixSec, CancellationToken.None);

        result.Should().BeEquivalentTo(
            testCase.Result, options => options.RespectingRuntimeTypes()
                .Using<double>(ctx =>
                    ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.0001))
                .WhenTypeIs<double>());
    }

    public static IEnumerable<object[]> TestCases => new EngineTestCase[]
    {
        new()
        {
            Query = "SUM(http_requests) BY (job) - COUNT(http_requests) BY (job)",
            Result =
                new VectorResult
                {
                    new Sample
                    {
                        Metric = new Labels { { "job", "api-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 996 },
                    },
                    new Sample
                    {
                        Metric = new Labels { { "job", "app-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 2596 },
                    }
                },
            StartTimestampUnixSec = 50 * 60
        },
        new()
        {
            Query = "2 - SUM(http_requests) BY (job)",
            Result =
                new VectorResult
                {
                    new Sample
                    {
                        Metric = new Labels { { "job", "api-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = -998 },
                    },
                    new Sample
                    {
                        Metric = new Labels { { "job", "app-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = -2598 },
                    }
                },
            StartTimestampUnixSec = 50 * 60
        },
        new()
        {
            Query = "-http_requests{job=\"api-server\",instance=\"0\",group=\"production\"}",
            Result =
                new VectorResult
                {
                    new Sample
                    {
                        Metric = new Labels
                        {
                            { "job", "api-server" }, { "instance", "0" }, { "group", "production" }
                        },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = -100 },
                    }
                },
            StartTimestampUnixSec = 50 * 60
        },
        new()
        {
            Query = "+http_requests{job=\"api-server\",instance=\"0\",group=\"production\"}",
            Result = new VectorResult
            {
                new Sample
                {
                    Metric = new Labels
                    {
                        { Labels.MetricName, "http_requests" },
                        { "job", "api-server" },
                        { "instance", "0" },
                        { "group", "production" }
                    },
                    Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 100 },
                }
            },
            StartTimestampUnixSec = 50 * 60
        },
        new()
        {
            Query = "- - - SUM(http_requests) BY (job)",
            Result =
                new VectorResult
                {
                    new Sample
                    {
                        Metric = new Labels { { "job", "api-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = -1000 },
                    },
                    new Sample
                    {
                        Metric = new Labels { { "job", "app-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = -2600 },
                    }
                },
            StartTimestampUnixSec = 50 * 60
        },
        new()
        {
            Query = "- - - 1",
            Result = new ScalarResult { TimestampUnixSec = 50 * 60, Value = -1 },
            StartTimestampUnixSec = 50 * 60
        },
        new()
        {
            Query = "1000 / SUM(http_requests) BY (job)",
            Result =
                new VectorResult
                {
                    new Sample
                    {
                        Metric = new Labels { { "job", "api-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 1 },
                    },
                    new Sample
                    {
                        Metric = new Labels { { "job", "app-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 0.38461538461538464 },
                    }
                },
            StartTimestampUnixSec = 50 * 60
        },
        new()
        {
            Query = "SUM(http_requests) BY (job) - 2",
            Result =
                new VectorResult
                {
                    new Sample
                    {
                        Metric = new Labels { { "job", "api-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 998 },
                    },
                    new Sample
                    {
                        Metric = new Labels { { "job", "app-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 2598 },
                    }
                },
            StartTimestampUnixSec = 50 * 60
        },
        new()
        {
            Query = "SUM(http_requests) BY (job) % 3",
            Result =
                new VectorResult
                {
                    new Sample
                    {
                        Metric = new Labels { { "job", "api-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 1 },
                    },
                    new Sample
                    {
                        Metric = new Labels { { "job", "app-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 2 },
                    }
                },
            StartTimestampUnixSec = 50 * 60
        },
        new()
        {
            Query = "SUM(http_requests) BY (job) % 0.3",
            Result = new VectorResult
            {
                new Sample
                {
                    Metric = new Labels { { "job", "api-server" } },
                    Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 0.1 },
                },
                new Sample
                {
                    Metric = new Labels { { "job", "app-server" } },
                    Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 0.2 },
                }
            },
            StartTimestampUnixSec = 50 * 60
        },
        new()
        {
            Query = "SUM(http_requests) BY (job) % 2 ^ (3 ^ 2)",
            Result = new VectorResult
            {
                new Sample
                {
                    Metric = new Labels { { "job", "api-server" } },
                    Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 488 },
                },
                new Sample
                {
                    Metric = new Labels { { "job", "app-server" } },
                    Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 40 },
                }
            },
            StartTimestampUnixSec = 50 * 60
        },
    }.Select(x => new object[] { x });
}
