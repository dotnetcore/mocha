// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Mocha.Core.Models.Metrics;
using Mocha.Core.Storage.Prometheus;
using Mocha.Query.Prometheus.PromQL.Engine;
using Mocha.Query.Prometheus.PromQL.Values;

namespace Mocha.Query.Tests.Prometheus.Engine.Functions;

public class IncreaseTests
{
    [Theory]
    [MemberData(nameof(TestCases))]
    public async Task Eval_Increase(EngineTestCase testCase)
    {
        var series = new[]
        {
            GenerateTimeSeries("http_requests{path=\"/foo\"}", TimeSpan.FromMinutes(5), 10, 0, 10), Merge(
                GenerateTimeSeries("http_requests{path=\"/bar\"}", TimeSpan.FromMinutes(5), 5, 0, 10),
                GenerateTimeSeries("http_requests{path=\"/bar\"}", 6 * 5 * 60, TimeSpan.FromMinutes(5), 5, 0, 10))
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
            await engine.QueryInstantAsync(
                testCase.Query, testCase.StartTimestampUnixSec, null, CancellationToken.None);

        result.Should().BeEquivalentTo(
            testCase.Result, options => options.RespectingRuntimeTypes()
                .Using<double>(ctx =>
                    ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.0001))
                .WhenTypeIs<double>());
    }

    public static IEnumerable<object[]> TestCases =
        new EngineTestCase[]
            {
                new()
                {
                    Query = "increase(http_requests[50m])",
                    Result = new VectorResult
                    {
                        new Sample
                        {
                            Metric = new Labels { { "path", "/foo" } },
                            Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 100 }
                        },
                        new Sample
                        {
                            Metric = new Labels { { "path", "/bar" } },
                            Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 90 }
                        },
                    },
                    StartTimestampUnixSec = 50 * 60,
                }
            }
            .Select(x => new object[] { x });
}
