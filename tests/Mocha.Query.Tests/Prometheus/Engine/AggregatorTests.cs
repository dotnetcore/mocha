// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Mocha.Core.Models.Metrics;
using Mocha.Core.Storage.Prometheus;
using Mocha.Core.Storage.Prometheus.Metrics;
using Mocha.Query.Prometheus.PromQL.Engine;
using Mocha.Query.Prometheus.PromQL.Values;

namespace Mocha.Query.Tests.Prometheus.Engine;

public class AggregatorTests
{
    [Theory]
    [MemberData(nameof(InstantAggregatorTestCases))]
    public async Task Eval_Instant_Aggregator(EngineTestCase testCase)
    {
        var series = new List<TimeSeries>
        {
            GenerateTimeSeries("http_requests{job=\"api-server\",instance=\"0\",group=\"production\"}",
                TimeSpan.FromMinutes(5), 10, 0, 10),
            GenerateTimeSeries("http_requests{job=\"api-server\",instance=\"0\",group=\"canary\"}",
                TimeSpan.FromMinutes(5), 10, 0, 30),
            GenerateTimeSeries("http_requests{job=\"api-server\",instance=\"1\",group=\"production\"}",
                TimeSpan.FromMinutes(5), 10, 0, 20),
            GenerateTimeSeries("http_requests{job=\"api-server\",instance=\"1\",group=\"canary\"}",
                TimeSpan.FromMinutes(5), 10, 0, 40),
            GenerateTimeSeries("http_requests{job=\"app-server\",instance=\"0\",group=\"production\"}",
                TimeSpan.FromMinutes(5), 10, 0, 50),
            GenerateTimeSeries("http_requests{job=\"app-server\",instance=\"1\",group=\"production\"}",
                TimeSpan.FromMinutes(5), 10, 0, 60),
            GenerateTimeSeries("http_requests{job=\"app-server\",instance=\"0\",group=\"canary\"}",
                TimeSpan.FromMinutes(5), 10, 0, 70),
            GenerateTimeSeries("http_requests{job=\"app-server\",instance=\"1\",group=\"canary\"}",
                TimeSpan.FromMinutes(5), 10, 0, 80)
        };

        var mockOptions = new Mock<IOptions<PromQLEngineOptions>>();
        mockOptions.SetupGet(x => x.Value).Returns(new PromQLEngineOptions
        {
            DefaultEvaluationInterval = TimeSpan.FromSeconds(15),
            MaxSamplesPerQuery = 50000000
        });

        var engine = new PromQLEngine(new MochaPromQLParserParser(), new InMemoryPrometheusMetricsReader(series),
            mockOptions.Object);

        var result =
            await engine.QueryInstantAsync(testCase.Query, testCase.StartTimestampUnixSec, null, CancellationToken.None);

        result.Should().BeEquivalentTo(
            testCase.Result, options => options.RespectingRuntimeTypes()
                .Using<double>(ctx =>
                    ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.0001))
                .WhenTypeIs<double>());
    }

    [Theory]
    [MemberData(nameof(InstantMinMaxTestCases))]
    public async Task Eval_Instant_Min_Max(EngineTestCase testCase)
    {
        var series = new List<TimeSeries>
        {
            new()
            {
                Labels = new Labels
                {
                    { Labels.MetricName, "http_requests" },
                    { "job", "api-server" },
                    { "instance", "0" },
                    { "group", "production" }
                },
                Samples =
                    new List<TimeSeriesSample> { new() { TimestampUnixSec = 0, Value = 1 } }
            },
            new()
            {
                Labels = new Labels
                {
                    { Labels.MetricName, "http_requests" },
                    { "job", "api-server" },
                    { "instance", "1" },
                    { "group", "production" }
                },
                Samples =
                    new List<TimeSeriesSample> { new() { TimestampUnixSec = 0, Value = 2 } }
            },
            new()
            {
                Labels = new Labels
                {
                    { Labels.MetricName, "http_requests" },
                    { "job", "api-server" },
                    { "instance", "0" },
                    { "group", "canary" }
                },
                Samples =
                    new List<TimeSeriesSample> { new() { TimestampUnixSec = 0, Value = double.NaN } }
            },
            new()
            {
                Labels = new Labels
                {
                    { Labels.MetricName, "http_requests" },
                    { "job", "api-server" },
                    { "instance", "1" },
                    { "group", "canary" }
                },
                Samples =
                    new List<TimeSeriesSample> { new() { TimestampUnixSec = 0, Value = 3 } }
            },
            new()
            {
                Labels = new Labels
                {
                    { Labels.MetricName, "http_requests" },
                    { "job", "api-server" },
                    { "instance", "2" },
                    { "group", "canary" }
                },
                Samples =
                    new List<TimeSeriesSample> { new() { TimestampUnixSec = 0, Value = 4 } }
            }
        };

        var mockOptions = new Mock<IOptions<PromQLEngineOptions>>();
        mockOptions.SetupGet(x => x.Value).Returns(new PromQLEngineOptions
        {
            DefaultEvaluationInterval = TimeSpan.FromSeconds(15),
            MaxSamplesPerQuery = 50000000
        });

        var engine = new PromQLEngine(new MochaPromQLParserParser(), new InMemoryPrometheusMetricsReader(series),
            mockOptions.Object);

        var result =
            await engine.QueryInstantAsync(testCase.Query, testCase.StartTimestampUnixSec, null, CancellationToken.None);

        result.Should().BeEquivalentTo(
            testCase.Result, options => options.RespectingRuntimeTypes()
                .Using<double>(ctx =>
                    ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.0001))
                .WhenTypeIs<double>());
    }

    [Theory]
    [MemberData(nameof(InstantTopKBottomKTestCases))]
    public async Task Eval_Instant_TopK_BottomK(EngineTestCase testCase)
    {
        var series = new TimeSeries[]
        {
            new()
            {
                Labels = new Labels
                {
                    { Labels.MetricName, "http_requests" },
                    { "job", "api-server" },
                    { "instance", "0" },
                    { "group", "production" }
                },
                Samples = new List<TimeSeriesSample> { new() { TimestampUnixSec = 50 * 60, Value = 100 } }
            },
            new()
            {
                Labels = new Labels
                {
                    { Labels.MetricName, "http_requests" },
                    { "job", "api-server" },
                    { "instance", "1" },
                    { "group", "production" }
                },
                Samples = new List<TimeSeriesSample> { new() { TimestampUnixSec = 50 * 60, Value = 200 } }
            },
            new()
            {
                Labels = new Labels
                {
                    { Labels.MetricName, "http_requests" },
                    { "job", "api-server" },
                    { "instance", "2" },
                    { "group", "production" }
                },
                Samples =
                    new List<TimeSeriesSample> { new() { TimestampUnixSec = 50 * 60, Value = double.NaN } }
            },
            new()
            {
                Labels = new Labels
                {
                    { Labels.MetricName, "http_requests" },
                    { "job", "api-server" },
                    { "instance", "0" },
                    { "group", "canary" }
                },
                Samples = new List<TimeSeriesSample> { new() { TimestampUnixSec = 50 * 60, Value = 300 } }
            },
            new()
            {
                Labels = new Labels
                {
                    { Labels.MetricName, "http_requests" },
                    { "job", "api-server" },
                    { "instance", "1" },
                    { "group", "canary" }
                },
                Samples = new List<TimeSeriesSample> { new() { TimestampUnixSec = 50 * 60, Value = 400 } }
            },
            new()
            {
                Labels = new Labels
                {
                    { Labels.MetricName, "http_requests" },
                    { "job", "app-server" },
                    { "instance", "0" },
                    { "group", "production" }
                },
                Samples = new List<TimeSeriesSample> { new() { TimestampUnixSec = 50 * 60, Value = 500 } }
            },
            new()
            {
                Labels = new Labels
                {
                    { Labels.MetricName, "http_requests" },
                    { "job", "app-server" },
                    { "instance", "1" },
                    { "group", "production" }
                },
                Samples =
                    new List<TimeSeriesSample> { new TimeSeriesSample { TimestampUnixSec = 50 * 60, Value = 600 } }
            },
            new()
            {
                Labels = new Labels
                {
                    { Labels.MetricName, "http_requests" },
                    { "job", "app-server" },
                    { "instance", "0" },
                    { "group", "canary" }
                },
                Samples = new List<TimeSeriesSample> { new() { TimestampUnixSec = 50 * 60, Value = 700 } }
            },
            new()
            {
                Labels = new Labels
                {
                    { Labels.MetricName, "http_requests" },
                    { "job", "app-server" },
                    { "instance", "1" },
                    { "group", "canary" }
                },
                Samples = new List<TimeSeriesSample> { new() { TimestampUnixSec = 50 * 60, Value = 800 } }
            }
        };

        var mockOptions = new Mock<IOptions<PromQLEngineOptions>>();
        mockOptions.SetupGet(x => x.Value).Returns(new PromQLEngineOptions
        {
            DefaultEvaluationInterval = TimeSpan.FromSeconds(15),
            MaxSamplesPerQuery = 50000000
        });

        var engine = new PromQLEngine(new MochaPromQLParserParser(), new InMemoryPrometheusMetricsReader(series),
            mockOptions.Object);

        var result =
            await engine.QueryInstantAsync(testCase.Query, testCase.StartTimestampUnixSec, null, CancellationToken.None);

        result.Should().BeEquivalentTo(
            testCase.Result, options => options.RespectingRuntimeTypes()
                .Using<double>(ctx =>
                    ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.0001))
                .WhenTypeIs<double>());
    }

    public static IEnumerable<object[]> InstantAggregatorTestCases = new EngineTestCase[]
    {
        // Simple sum
        new()
        {
            Query = "sum(http_requests{job=\"api-server\"}) by (group)",
            Result = new VectorResult
            {
                new Sample
                {
                    Metric = new Labels { { "group", "canary" } },
                    Point = new DoublePoint { Value = 700, TimestampUnixSec = 50 * 60 }
                },
                new Sample
                {
                    Metric = new Labels { { "group", "production" } },
                    Point = new DoublePoint { Value = 300, TimestampUnixSec = 50 * 60 }
                }
            },
            StartTimestampUnixSec = 50 * 60
        },
        // # Test alternative "by"-clause order
        new()
        {
            Query = "sum by (group) (http_requests{job=\"api-server\"})",
            Result = new VectorResult
            {
                new Sample
                {
                    Metric = new Labels { { "group", "canary" } },
                    Point = new DoublePoint { Value = 700, TimestampUnixSec = 50 * 60 }
                },
                new Sample
                {
                    Metric = new Labels { { "group", "production" } },
                    Point = new DoublePoint { Value = 300, TimestampUnixSec = 50 * 60 }
                }
            },
            StartTimestampUnixSec = 50 * 60
        },
        // Simple average
        new()
        {
            Query = "avg(http_requests{job=\"api-server\"}) by (group)",
            Result = new VectorResult
            {
                new Sample
                {
                    Metric = new Labels { { "group", "canary" } },
                    Point = new DoublePoint { Value = 350, TimestampUnixSec = 50 * 60 }
                },
                new Sample
                {
                    Metric = new Labels { { "group", "production" } },
                    Point = new DoublePoint { Value = 150, TimestampUnixSec = 50 * 60 }
                }
            },
            StartTimestampUnixSec = 50 * 60
        },
        // Simple count
        new()
        {
            Query = "count(http_requests{job=\"api-server\"}) by (group)",
            Result = new VectorResult
            {
                new Sample
                {
                    Metric = new Labels { { "group", "canary" } },
                    Point = new DoublePoint { Value = 2, TimestampUnixSec = 50 * 60 }
                },
                new Sample
                {
                    Metric = new Labels { { "group", "production" } },
                    Point = new DoublePoint { Value = 2, TimestampUnixSec = 50 * 60 }
                }
            },
            StartTimestampUnixSec = 50 * 60
        },
        // Simple without
        new()
        {
            Query = "sum without (instance) (http_requests{job=\"api-server\"})",
            Result = new VectorResult
            {
                new Sample
                {
                    Metric = new Labels { { "group", "canary" }, { "job", "api-server" } },
                    Point = new DoublePoint { Value = 700, TimestampUnixSec = 50 * 60 }
                },
                new Sample
                {
                    Metric = new Labels { { "group", "production" }, { "job", "api-server" } },
                    Point = new DoublePoint { Value = 300, TimestampUnixSec = 50 * 60 }
                }
            },
            StartTimestampUnixSec = 50 * 60
        },
        // Empty by
        new()
        {
            Query = "sum by () (http_requests{job=\"api-server\"})",
            Result = new VectorResult
            {
                new Sample
                {
                    Metric = new Labels(),
                    Point = new DoublePoint { Value = 1000, TimestampUnixSec = 50 * 60 }
                }
            },
            StartTimestampUnixSec = 50 * 60
        },
        // No by/without
        new()
        {
            Query = "sum(http_requests{job=\"api-server\"})",
            Result = new VectorResult
            {
                new Sample
                {
                    Metric = new Labels(),
                    Point = new DoublePoint { Value = 1000, TimestampUnixSec = 50 * 60 }
                }
            },
            StartTimestampUnixSec = 50 * 60
        },
        // Empty without
        new()
        {
            Query = "sum without () (http_requests{job=\"api-server\",group=\"production\"})",
            Result = new VectorResult
            {
                new Sample
                {
                    Metric = new Labels
                    {
                        { "group", "production" }, { "job", "api-server" }, { "instance", "0" }
                    },
                    Point = new DoublePoint { Value = 100, TimestampUnixSec = 50 * 60 }
                },
                new Sample
                {
                    Metric = new Labels
                    {
                        { "group", "production" }, { "job", "api-server" }, { "instance", "1" }
                    },
                    Point = new DoublePoint { Value = 200, TimestampUnixSec = 50 * 60 }
                }
            },
            StartTimestampUnixSec = 50 * 60
        },
        // TODO
        // Without with mismatched and missing labels
        // new()
        // {
        //     Query = "sum without (instance) (http_requests{job=\"api-server\"} or foo)",
        //     Result = new VectorResult
        //     {
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "canary" }, { "job", "api-server" } },
        //             Point = new DoublePoint { Value = 700, TimestampUnixSec = 50 * 60 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "production" }, { "job", "api-server" } },
        //             Point = new DoublePoint { Value = 300, TimestampUnixSec = 50 * 60 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "region", "europe" }, { "job", "api-server" } },
        //             Point = new DoublePoint { Value = 900, TimestampUnixSec = 50 * 60 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "job", "api-server" } },
        //             Point = new DoublePoint { Value = 1000, TimestampUnixSec = 50 * 60 }
        //         }
        //     },
        //     StartTimestampUnixSec = 50 * 60
        // },
        // Lower-cased aggregation operators should work too
        new()
        {
            Query =
                "sum(http_requests) by (job) + min(http_requests) by (job) + max(http_requests) by (job) + avg(http_requests) by (job)",
            Result = new VectorResult
            {
                new Sample
                {
                    Metric = new Labels { { "job", "app-server" } },
                    Point = new DoublePoint { Value = 4550, TimestampUnixSec = 50 * 60 }
                },
                new Sample
                {
                    Metric = new Labels { { "job", "api-server" } },
                    Point = new DoublePoint { Value = 1750, TimestampUnixSec = 50 * 60 }
                }
            },
            StartTimestampUnixSec = 50 * 60
        },

        // TODO
        // Test both alternative "by"-clause orders in one expression
        // new()
        // {
        //     Query = "sum(sum by (group) (http_requests{job=\"api-server\"})) by (job)",
        //     Result = new VectorResult
        //     {
        //         new Sample
        //         {
        //             Metric = new Labels { { "job", "app-server" } },
        //             Point = new DoublePoint { Value = 4550, TimestampUnixSec = 50 * 60 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "job", "api-server" } },
        //             Point = new DoublePoint { Value = 1750, TimestampUnixSec = 50 * 60 }
        //         }
        //     },
        //     StartTimestampUnixSec = 50 * 60
        // }
    }.Select(x => new object[] { x });

    public static IEnumerable<object[]> InstantMinMaxTestCases = new EngineTestCase[]
    {
        new()
        {
            Query = "max(http_requests)",
            Result =
                new VectorResult
                {
                    new Sample
                    {
                        Metric = Labels.Empty, Point = new DoublePoint { Value = 4, TimestampUnixSec = 0 }
                    }
                },
            StartTimestampUnixSec = 0
        },
        new()
        {
            Query = "min(http_requests)",
            Result =
                new VectorResult
                {
                    new Sample
                    {
                        Metric = Labels.Empty, Point = new DoublePoint { Value = 1, TimestampUnixSec = 0 }
                    }
                },
            StartTimestampUnixSec = 0
        },
        new()
        {
            Query = "max by (group) (http_requests)",
            Result = new VectorResult
            {
                new Sample
                {
                    Metric = new Labels { { "group", "production" } },
                    Point = new DoublePoint { Value = 2, TimestampUnixSec = 0 }
                },
                new Sample
                {
                    Metric = new Labels { { "group", "canary" } },
                    Point = new DoublePoint { Value = 4, TimestampUnixSec = 0 }
                }
            },
            StartTimestampUnixSec = 0
        },
        new()
        {
            Query = "min by (group) (http_requests)",
            Result = new VectorResult
            {
                new Sample
                {
                    Metric = new Labels { { "group", "production" } },
                    Point = new DoublePoint { Value = 1, TimestampUnixSec = 0 }
                },
                new Sample
                {
                    Metric = new Labels { { "group", "canary" } },
                    Point = new DoublePoint { Value = 3, TimestampUnixSec = 0 }
                }
            },
            StartTimestampUnixSec = 0
        }
    }.Select(x => new object[] { x });

    public static IEnumerable<object[]> InstantTopKBottomKTestCases = new EngineTestCase[]
    {
        new()
        {
            Query = "topk(3, http_requests)",
            Result =
                new VectorResult
                {
                    new Sample
                    {
                        Metric = new Labels
                        {
                            { Labels.MetricName, "http_requests" },
                            { "group", "canary" },
                            { "instance", "1" },
                            { "job", "app-server" }
                        },
                        Point = new DoublePoint { Value = 800, TimestampUnixSec = 50 * 60 }
                    },
                    new Sample
                    {
                        Metric = new Labels
                        {
                            { Labels.MetricName, "http_requests" },
                            { "group", "canary" },
                            { "instance", "0" },
                            { "job", "app-server" }
                        },
                        Point = new DoublePoint { Value = 700, TimestampUnixSec = 50 * 60 }
                    },
                    new Sample
                    {
                        Metric = new Labels
                        {
                            { Labels.MetricName, "http_requests" },
                            { "group", "production" },
                            { "instance", "1" },
                            { "job", "app-server" }
                        },
                        Point = new DoublePoint { Value = 600, TimestampUnixSec = 50 * 60 }
                    }
                },
            StartTimestampUnixSec = 50 * 60
        },
        new()
        {
            Query = "topk(5, http_requests{group=\"canary\",job=\"app-server\"})",
            Result =
                new VectorResult
                {
                    new Sample
                    {
                        Metric = new Labels
                        {
                            { Labels.MetricName, "http_requests" },
                            { "group", "canary" },
                            { "instance", "1" },
                            { "job", "app-server" }
                        },
                        Point = new DoublePoint { Value = 800, TimestampUnixSec = 50 * 60 },
                    },
                    new Sample
                    {
                        Metric = new Labels
                        {
                            { Labels.MetricName, "http_requests" },
                            { "group", "canary" },
                            { "instance", "0" },
                            { "job", "app-server" }
                        },
                        Point = new DoublePoint { Value = 700, TimestampUnixSec = 50 * 60 }
                    }
                },
            StartTimestampUnixSec = 50 * 60
        },
        new()
        {
            Query = "bottomk(2, http_requests{group=\"canary\"})",
            Result = new VectorResult
            {
                new Sample
                {
                    Metric = new Labels
                    {
                        { Labels.MetricName, "http_requests" },
                        { "group", "canary" },
                        { "instance", "0" },
                        { "job", "api-server" }
                    },
                    Point = new DoublePoint { Value = 300, TimestampUnixSec = 50 * 60 }
                },
                new Sample
                {
                    Metric = new Labels
                    {
                        { Labels.MetricName, "http_requests" },
                        { "group", "canary" },
                        { "instance", "1" },
                        { "job", "api-server" }
                    },
                    Point = new DoublePoint { Value = 400, TimestampUnixSec = 50 * 60 }
                }
            },
            StartTimestampUnixSec = 50 * 60
        }
    }.Select(x => new object[] { x });
}
