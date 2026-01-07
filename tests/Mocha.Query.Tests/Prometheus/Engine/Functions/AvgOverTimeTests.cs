// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Mocha.Core.Models.Metrics;
using Mocha.Query.Prometheus.PromQL.Engine;
using Mocha.Query.Prometheus.PromQL.Values;

namespace Mocha.Query.Tests.Prometheus.Engine.Functions;

public class AvgOverTimeTests
{
    [Fact]
    public async Task Eval_Avg_Over_Time()
    {
        var series = GenerateTimeSeries("metric", TimeSpan.FromSeconds(10), 4, 1, 1);

        var mockOptions = new Mock<IOptions<PromQLEngineOptions>>();
        mockOptions.SetupGet(x => x.Value).Returns(new PromQLEngineOptions
        {
            DefaultEvaluationInterval = TimeSpan.FromSeconds(15),
            MaxSamplesPerQuery = 50000000
        });

        var engine = new PromQLEngine(
            new MochaPromQLParserParser(),
            new InMemoryPrometheusMetricsReader([series]),
            mockOptions.Object);

        var result = await engine.QueryInstantAsync(
            "avg_over_time(metric[1m])",
            60,
            null,
            CancellationToken.None);

        result.Should().BeEquivalentTo(
            new VectorResult
            {
                new Sample { Metric = new Labels(), Point = new DoublePoint { TimestampUnixSec = 60, Value = 3 } }
            },
            options => options.RespectingRuntimeTypes()
                .Using<double>(ctx =>
                    ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.0001))
                .WhenTypeIs<double>());
    }
}
