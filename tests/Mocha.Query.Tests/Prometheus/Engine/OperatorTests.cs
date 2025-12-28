// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Mocha.Core.Models.Metrics;
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
        if (testCase == null)
        {
            throw new ArgumentNullException(nameof(testCase));
        }

        if (testCase == null)
        {
            throw new ArgumentNullException(nameof(testCase));
        }

        var series = new[]
        {
            GenerateTimeSeries("http_requests{job=\"api-server\",instance=\"0\",group=\"production\"}",
                TimeSpan.FromMinutes(5), 10, 0, 10),
            GenerateTimeSeries("http_requests{job=\"api-server\",instance=\"1\",group=\"production\"}",
                TimeSpan.FromMinutes(5), 10, 0, 20),
            GenerateTimeSeries("http_requests{job=\"api-server\",instance=\"0\",group=\"canary\"}",
                TimeSpan.FromMinutes(5), 10, 0, 30),
            GenerateTimeSeries("http_requests{job=\"api-server\",instance=\"1\",group=\"canary\"}"
                , TimeSpan.FromMinutes(5), 10, 0, 40),
            GenerateTimeSeries("http_requests{job=\"app-server\",instance=\"0\",group=\"production\"}",
                TimeSpan.FromMinutes(5), 10, 0, 50),
            GenerateTimeSeries("http_requests{job=\"app-server\",instance=\"1\",group=\"production\"}",
                TimeSpan.FromMinutes(5), 10, 0, 60),
            GenerateTimeSeries("http_requests{job=\"app-server\",instance=\"0\",group=\"canary\"}",
                TimeSpan.FromMinutes(5), 10, 0, 70),
            GenerateTimeSeries("http_requests{job=\"app-server\",instance=\"1\",group=\"canary\"}",
                TimeSpan.FromMinutes(5), 10, 0, 80),
            GenerateTimeSeries("vector_matching_a{l=\"x\"}", TimeSpan.FromMinutes(1), 100, 0, 1),
            GenerateTimeSeries("vector_matching_a{l=\"y\"}", TimeSpan.FromMinutes(1), 50, 0, 2),
            GenerateTimeSeries("vector_matching_b{l=\"x\"}", TimeSpan.FromMinutes(1), 25, 0, 4)
        };

        var mockOptions = new Mock<IOptions<PromQLEngineOptions>>();
        mockOptions.SetupGet(x => x.Value).Returns(new PromQLEngineOptions
        {
            DefaultEvaluationInterval = TimeSpan.FromSeconds(15), MaxSamplesPerQuery = 50000000
        });

        var engine = new PromQLEngine(new MochaPromQLParserParser(), new InMemoryPrometheusMetricsReader(series),
            mockOptions.Object);

        var result =
            await engine.QueryInstantAsync(
                testCase.Query, testCase.StartTimestampUnixSec, null, CancellationToken.None);

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
            StartTimestampUnixSec = 50 * 60,
            Result =
                new VectorResult
                {
                    new Sample
                    {
                        Metric = new Labels { { "job", "api-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 996 }
                    },
                    new Sample
                    {
                        Metric = new Labels { { "job", "app-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 2596 }
                    },
                }
        },
        new()
        {
            Query = "2 - SUM(http_requests) BY (job)",
            StartTimestampUnixSec = 50 * 60,
            Result =
                new VectorResult
                {
                    new Sample
                    {
                        Metric = new Labels { { "job", "api-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = -998 }
                    },
                    new Sample
                    {
                        Metric = new Labels { { "job", "app-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = -2598 }
                    },
                }
        },
        new()
        {
            Query = "-http_requests{job=\"api-server\",instance=\"0\",group=\"production\"}",
            StartTimestampUnixSec = 50 * 60,
            Result =
                new VectorResult
                {
                    new Sample
                    {
                        Metric = new Labels
                        {
                            { "job", "api-server" }, { "instance", "0" }, { "group", "production" }
                        },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = -100 }
                    }
                }
        },
        // TODO: MetricName should dropped from labels
        // new()
        // {
        //     Query = "+http_requests{job=\"api-server\",instance=\"0\",group=\"production\"}",
        //     StartTimestampUnixSec = 50 * 60,
        //     Result =
        //         new VectorResult
        //         {
        //             new Sample
        //             {
        //                 Metric = new Labels
        //                 {
        //                     { "job", "api-server" }, { "instance", "0" }, { "group", "production" }
        //                 },
        //                 Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 100 }
        //             }
        //         }
        // },
        new()
        {
            Query = "- - - SUM(http_requests) BY (job)",
            StartTimestampUnixSec = 50 * 60,
            Result =
                new VectorResult
                {
                    new Sample
                    {
                        Metric = new Labels { { "job", "api-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = -1000 }
                    },
                    new Sample
                    {
                        Metric = new Labels { { "job", "app-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = -2600 }
                    },
                }
        },
        new()
        {
            Query = "- - - 1",
            StartTimestampUnixSec = 50 * 60,
            Result = new ScalarResult { TimestampUnixSec = 50 * 60, Value = -1 }
        },
        new()
        {
            Query = "1000 / SUM(http_requests) BY (job)",
            StartTimestampUnixSec = 50 * 60,
            Result =
                new VectorResult
                {
                    new Sample
                    {
                        Metric = new Labels { { "job", "api-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 1 }
                    },
                    new Sample
                    {
                        Metric = new Labels { { "job", "app-server" } },
                        Point =
                            new DoublePoint { TimestampUnixSec = 50 * 60, Value = 0.38461538461538464 }
                    },
                }
        },
        new()
        {
            Query = "SUM(http_requests) BY (job) - 2",
            StartTimestampUnixSec = 50 * 60,
            Result =
                new VectorResult
                {
                    new Sample
                    {
                        Metric = new Labels { { "job", "api-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 998 }
                    },
                    new Sample
                    {
                        Metric = new Labels { { "job", "app-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 2598 }
                    },
                }
        },
        new()
        {
            Query = "SUM(http_requests) BY (job) % 3",
            StartTimestampUnixSec = 50 * 60,
            Result =
                new VectorResult
                {
                    new Sample
                    {
                        Metric = new Labels { { "job", "api-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 1 }
                    },
                    new Sample
                    {
                        Metric = new Labels { { "job", "app-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 2 }
                    },
                }
        },
        new()
        {
            Query = "SUM(http_requests) BY (job) % 0.3",
            StartTimestampUnixSec = 50 * 60,
            Result =
                new VectorResult
                {
                    new Sample
                    {
                        Metric = new Labels { { "job", "api-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 0.1 }
                    },
                    new Sample
                    {
                        Metric = new Labels { { "job", "app-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 0.2 }
                    },
                }
        },
        new()
        {
            Query = "SUM(http_requests) BY (job) ^ 2",
            StartTimestampUnixSec = 50 * 60,
            Result =
                new VectorResult
                {
                    new Sample
                    {
                        Metric = new Labels { { "job", "api-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 1000000 }
                    },
                    new Sample
                    {
                        Metric = new Labels { { "job", "app-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 6760000 }
                    },
                }
        },
        new()
        {
            Query = "COUNT(http_requests) BY (job) ^ COUNT(http_requests) BY (job)",
            StartTimestampUnixSec = 50 * 60,
            Result =
                new VectorResult
                {
                    new Sample
                    {
                        Metric = new Labels { { "job", "api-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 256 }
                    },
                    new Sample
                    {
                        Metric = new Labels { { "job", "app-server" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 256 }
                    },
                }
        },
        new()
        {
            Query = "SUM(http_requests) BY (job) / 0",
            StartTimestampUnixSec = 50 * 60,
            Result =
                new VectorResult
                {
                    new Sample
                    {
                        Metric = new Labels { { "job", "api-server" } },
                        Point =
                            new DoublePoint { TimestampUnixSec = 50 * 60, Value = double.PositiveInfinity }
                    },
                    new Sample
                    {
                        Metric = new Labels { { "job", "app-server" } },
                        Point =
                            new DoublePoint { TimestampUnixSec = 50 * 60, Value = double.PositiveInfinity }
                    },
                }
        },
        new()
        {
            Query = "SUM(http_requests) BY (job) == bool 1000",
            StartTimestampUnixSec = 50 * 60,
            Result = new VectorResult
            {
                new Sample
                {
                    Metric = new Labels { { "job", "api-server" } },
                    Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 1 }
                },
                new Sample
                {
                    Metric = new Labels { { "job", "app-server" } },
                    Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 0 }
                },
            }
        },
        new()
        {
            Query = "0 == bool 1",
            StartTimestampUnixSec = 50 * 60,
            Result = new ScalarResult { TimestampUnixSec = 50 * 60, Value = 0 }
        },
        new()
        {
            Query = "1 == bool 1",
            StartTimestampUnixSec = 50 * 60,
            Result = new ScalarResult { TimestampUnixSec = 50 * 60, Value = 1 }
        },
        // and / or / unless (vector matching)
        // TODO: MetricName should dropped from labels
        // new()
        // {
        //     Query = "http_requests{group=\"canary\"} and http_requests{instance=\"0\"}",
        //     StartTimestampUnixSec = 50 * 60,
        //     Result =
        //         new VectorResult
        //         {
        //             new Sample
        //             {
        //                 Metric = new Labels
        //                 {
        //                     { "group", "canary" }, { "instance", "0" }, { "job", "api-server" }
        //                 },
        //                 Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 300 }
        //             },
        //             new Sample
        //             {
        //                 Metric = new Labels
        //                 {
        //                     { "group", "canary" }, { "instance", "0" }, { "job", "app-server" }
        //                 },
        //                 Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 700 }
        //             }
        //         }
        // },
        // new()
        // {
        //     Query = "(http_requests{group=\"canary\"} + 1) and http_requests{instance=\"0\"}",
        //     StartTimestampUnixSec = 50 * 60,
        //     Result =
        //         new VectorResult
        //         {
        //             new Sample
        //             {
        //                 Metric = new Labels
        //                 {
        //                     { "group", "canary" }, { "instance", "0" }, { "job", "api-server" }
        //                 },
        //                 Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 301 }
        //             },
        //             new Sample
        //             {
        //                 Metric = new Labels
        //                 {
        //                     { "group", "canary" }, { "instance", "0" }, { "job", "app-server" }
        //                 },
        //                 Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 701 }
        //             }
        //         }
        // },
        // new()
        // {
        //     Query =
        //         "(http_requests{group=\"canary\"} + 1) and on(instance, job) http_requests{instance=\"0\", group=\"production\"}",
        //     StartTimestampUnixSec = 50 * 60,
        //     Result =
        //         new VectorResult
        //         {
        //             new Sample
        //             {
        //                 Metric = new Labels
        //                 {
        //                     { "group", "canary" }, { "instance", "0" }, { "job", "api-server" }
        //                 },
        //                 Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 301 }
        //             },
        //             new Sample
        //             {
        //                 Metric = new Labels
        //                 {
        //                     { "group", "canary" }, { "instance", "0" }, { "job", "app-server" }
        //                 },
        //                 Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 701 }
        //             }
        //         }
        // },
        // new()
        // {
        //     Query =
        //         "(http_requests{group=\"canary\"} + 1) and on(instance) http_requests{instance=\"0\", group=\"production\"}",
        //     StartTimestampUnixSec = 50 * 60,
        //     Result =
        //         new VectorResult
        //         {
        //             new Sample
        //             {
        //                 Metric = new Labels
        //                 {
        //                     { "group", "canary" }, { "instance", "0" }, { "job", "api-server" }
        //                 },
        //                 Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 301 }
        //             },
        //             new Sample
        //             {
        //                 Metric = new Labels
        //                 {
        //                     { "group", "canary" }, { "instance", "0" }, { "job", "app-server" }
        //                 },
        //                 Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 701 }
        //             }
        //         }
        // },
        // new()
        // {
        //     Query =
        //         "(http_requests{group=\"canary\"} + 1) and ignoring(group) http_requests{instance=\"0\", group=\"production\"}",
        //     StartTimestampUnixSec = 50 * 60,
        //     Result =
        //         new VectorResult
        //         {
        //             new Sample
        //             {
        //                 Metric = new Labels
        //                 {
        //                     { "group", "canary" }, { "instance", "0" }, { "job", "api-server" }
        //                 },
        //                 Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 301 }
        //             },
        //             new Sample
        //             {
        //                 Metric = new Labels
        //                 {
        //                     { "group", "canary" }, { "instance", "0" }, { "job", "app-server" }
        //                 },
        //                 Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 701 }
        //             }
        //         }
        // },
        // new()
        // {
        //     Query =
        //         "(http_requests{group=\"canary\"} + 1) and ignoring(group, job) http_requests{instance=\"0\", group=\"production\"}",
        //     StartTimestampUnixSec = 50 * 60,
        //     Result = new VectorResult
        //     {
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "canary" }, { "instance", "0" }, { "job", "api-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 301 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "canary" }, { "instance", "0" }, { "job", "app-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 701 }
        //         }
        //     }
        // },
        // // or
        // new()
        // {
        //     Query = "http_requests{group=\"canary\"} or http_requests{group=\"production\"}",
        //     StartTimestampUnixSec = 50 * 60,
        //     Result = new VectorResult
        //     {
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "canary" }, { "instance", "0" }, { "job", "api-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 300 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "canary" }, { "instance", "0" }, { "job", "app-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 700 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "canary" }, { "instance", "1" }, { "job", "api-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 400 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "canary" }, { "instance", "1" }, { "job", "app-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 800 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "production" }, { "instance", "0" }, { "job", "api-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 100 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "production" }, { "instance", "0" }, { "job", "app-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 500 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "production" }, { "instance", "1" }, { "job", "api-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 200 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "production" }, { "instance", "1" }, { "job", "app-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 600 }
        //         },
        //     }
        // },
        // On overlap the rhs samples must be dropped.
        // TODO: MetricName should dropped from labels
        // new()
        // {
        //     Query = "(http_requests{group=\"canary\"} + 1) or http_requests{instance=\"1\"}",
        //     StartTimestampUnixSec = 50 * 60,
        //     Result = new VectorResult
        //     {
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "canary" }, { "instance", "0" }, { "job", "api-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 301 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "canary" }, { "instance", "0" }, { "job", "app-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 701 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "canary" }, { "instance", "1" }, { "job", "api-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 401 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "canary" }, { "instance", "1" }, { "job", "app-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 801 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "production" }, { "instance", "1" }, { "job", "api-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 200 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "production" }, { "instance", "1" }, { "job", "app-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 600 }
        //         },
        //     }
        // },
        // unless
        // TODO: MetricName should dropped from labels
        // new()
        // {
        //     Query = "http_requests{group=\"canary\"} unless http_requests{instance=\"0\"}",
        //     StartTimestampUnixSec = 50 * 60,
        //     Result =
        //         new VectorResult
        //         {
        //             new Sample
        //             {
        //                 Metric = new Labels
        //                 {
        //                     { "group", "canary" }, { "instance", "1" }, { "job", "api-server" }
        //                 },
        //                 Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 400 }
        //             },
        //             new Sample
        //             {
        //                 Metric = new Labels
        //                 {
        //                     { "group", "canary" }, { "instance", "1" }, { "job", "app-server" }
        //                 },
        //                 Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 800 }
        //             },
        //         }
        // },
        // new()
        // {
        //     Query = "http_requests{group=\"canary\"} unless on(job) http_requests{instance=\"0\"}",
        //     StartTimestampUnixSec = 50 * 60,
        //     Result = new VectorResult()
        // },
        // new()
        // {
        //     Query = "http_requests{group=\"canary\"} unless on(job, instance) http_requests{instance=\"0\"}",
        //     StartTimestampUnixSec = 50 * 60,
        //     Result =
        //         new VectorResult
        //         {
        //             new Sample
        //             {
        //                 Metric = new Labels
        //                 {
        //                     { "group", "canary" }, { "instance", "1" }, { "job", "api-server" }
        //                 },
        //                 Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 400 }
        //             },
        //             new Sample
        //             {
        //                 Metric = new Labels
        //                 {
        //                     { "group", "canary" }, { "instance", "1" }, { "job", "app-server" }
        //                 },
        //                 Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 800 }
        //             },
        //         }
        // },
        // new()
        // {
        //     Query =
        //         "http_requests{group=\"canary\"} unless ignoring(group, instance) http_requests{instance=\"0\"}",
        //     StartTimestampUnixSec = 50 * 60,
        //     Result = new VectorResult()
        // },
        // new()
        // {
        //     Query = "http_requests{group=\"canary\"} unless ignoring(group) http_requests{instance=\"0\"}",
        //     StartTimestampUnixSec = 50 * 60,
        //     Result =
        //         new VectorResult
        //         {
        //             new Sample
        //             {
        //                 Metric = new Labels
        //                 {
        //                     { "group", "canary" }, { "instance", "1" }, { "job", "api-server" }
        //                 },
        //                 Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 400 }
        //             },
        //             new Sample
        //             {
        //                 Metric = new Labels
        //                 {
        //                     { "group", "canary" }, { "instance", "1" }, { "job", "app-server" }
        //                 },
        //                 Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 800 }
        //             },
        //         }
        // },
        // TODO: support vector(1)
        // new()
        // {
        //     Query = "http_requests AND ON (dummy) vector(1)",
        //     StartTimestampUnixSec = 50 * 60,
        //     Result = new VectorResult
        //     {
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "canary" }, { "instance", "0" }, { "job", "api-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 300 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "canary" }, { "instance", "0" }, { "job", "app-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 700 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "canary" }, { "instance", "1" }, { "job", "api-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 400 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "canary" }, { "instance", "1" }, { "job", "app-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 800 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "production" }, { "instance", "0" }, { "job", "api-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 100 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "production" }, { "instance", "0" }, { "job", "app-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 500 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "production" }, { "instance", "1" }, { "job", "api-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 200 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "production" }, { "instance", "1" }, { "job", "app-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 600 }
        //         },
        //     }
        // },
        // new()
        // {
        //     Query = "http_requests AND IGNORING (group, instance, job) vector(1)",
        //     StartTimestampUnixSec = 50 * 60,
        //     Result = new VectorResult
        //     {
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "canary" }, { "instance", "0" }, { "job", "api-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 300 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "canary" }, { "instance", "0" }, { "job", "app-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 700 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "canary" }, { "instance", "1" }, { "job", "api-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 400 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "canary" }, { "instance", "1" }, { "job", "app-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 800 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "production" }, { "instance", "0" }, { "job", "api-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 100 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "production" }, { "instance", "0" }, { "job", "app-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 500 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "production" }, { "instance", "1" }, { "job", "api-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 200 }
        //         },
        //         new Sample
        //         {
        //             Metric = new Labels { { "group", "production" }, { "instance", "1" }, { "job", "app-server" } },
        //             Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 600 }
        //         },
        //     }
        // },
    }.Select(x => new object[] { x });
}
