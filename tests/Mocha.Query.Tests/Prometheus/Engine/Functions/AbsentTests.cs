// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Mocha.Core.Models.Metrics;
using Mocha.Query.Prometheus.PromQL.Engine;
using Mocha.Query.Prometheus.PromQL.Values;

namespace Mocha.Query.Tests.Prometheus.Engine.Functions;

public class AbsentTests
{
    [Theory]
    [MemberData(nameof(TestCases))]
    public async Task Eval_Absent(EngineTestCase testCase)
    {
        var series = new[]
        {
            GenerateTimeSeries(
                "http_requests{job=\"api-server\", instance=\"0\", group=\"production\"}",
                TimeSpan.FromMinutes(5), 10, 0, 10),

            GenerateTimeSeries(
                "http_requests{job=\"api-server\", instance=\"1\", group=\"production\"}",
                TimeSpan.FromMinutes(5), 10, 0, 20),

            GenerateTimeSeries(
                "http_requests{job=\"api-server\", instance=\"0\", group=\"canary\"}",
                TimeSpan.FromMinutes(5), 10, 0, 30),

            GenerateTimeSeries(
                "http_requests{job=\"api-server\", instance=\"1\", group=\"canary\"}",
                TimeSpan.FromMinutes(5), 10, 0, 40),

            GenerateTimeSeries(
                "http_requests{job=\"app-server\", instance=\"0\", group=\"production\"}",
                TimeSpan.FromMinutes(5), 10, 0, 50),

            GenerateTimeSeries(
                "http_requests{job=\"app-server\", instance=\"1\", group=\"production\"}",
                TimeSpan.FromMinutes(5), 10, 0, 60),

            GenerateTimeSeries(
                "http_requests{job=\"app-server\", instance=\"0\", group=\"canary\"}",
                TimeSpan.FromMinutes(5), 10, 0, 70),

            GenerateTimeSeries(
                "http_requests{job=\"app-server\", instance=\"1\", group=\"canary\"}",
                TimeSpan.FromMinutes(5), 10, 0, 80)
        };

        var engine = new PromQLEngine(
            new MochaPromQLParserParser(),
            new InMemoryPrometheusMetricsReader(series),
            Options.Create(new PromQLEngineOptions()));

        var result = await engine.QueryInstantAsync(
            testCase.Query,
            testCase.StartTimestampUnixSec,
            null,
            CancellationToken.None);

        result.Should().BeEquivalentTo(
            testCase.Result,
            options => options.RespectingRuntimeTypes());
    }

    public static IEnumerable<object[]> TestCases =
        new EngineTestCase[]
        {
            new()
            {
                Query = "absent(nonexistent)",
                StartTimestampUnixSec = 50 * 60,
                Result = new VectorResult
                {
                    new Sample
                    {
                        Metric = new Labels(),
                        Point = new DoublePoint
                        {
                            TimestampUnixSec = 50 * 60,
                            Value = 1
                        }
                    }
                }
            },

            new()
            {
                Query =
                    "absent(nonexistent{job=\"testjob\", instance=\"testinstance\", method=~\".x\"})",
                StartTimestampUnixSec = 50 * 60,
                Result = new VectorResult
                {
                    new Sample
                    {
                        Metric = new Labels
                        {
                            { "job", "testjob" },
                            { "instance", "testinstance" }
                        },
                        Point = new DoublePoint
                        {
                            TimestampUnixSec = 50 * 60,
                            Value = 1
                        }
                    }
                }
            },

            new()
            {
                Query = "absent(http_requests)",
                StartTimestampUnixSec = 50 * 60,
                Result = new VectorResult()
            },

            new()
            {
                Query = "absent(sum(http_requests))",
                StartTimestampUnixSec = 50 * 60,
                Result = new VectorResult()
            },

            new()
            {
                Query = "absent(sum(nonexistent{job=\"testjob\", instance=\"testinstance\"}))",
                StartTimestampUnixSec = 50 * 60,
                Result = new VectorResult
                {
                    new Sample
                    {
                        Metric = new Labels(),
                        Point = new DoublePoint
                        {
                            TimestampUnixSec = 50 * 60,
                            Value = 1
                        }
                    }
                }
            }
        }.Select(x => new object[] { x });
}
