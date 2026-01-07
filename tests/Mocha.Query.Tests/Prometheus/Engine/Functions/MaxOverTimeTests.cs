// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Mocha.Core.Models.Metrics;
using Mocha.Query.Prometheus.PromQL.Engine;
using Mocha.Query.Prometheus.PromQL.Values;

namespace Mocha.Query.Tests.Prometheus.Engine.Functions;

public class MaxOverTimeTests
{
    [Fact]
    public async Task Eval_Max_Over_Time_With_NaN()
    {
        var series = new[]
        {
            GenerateTimeSeries(
                "data{type=\"numbers\"}", 0, TimeSpan.FromSeconds(10), 2d, 0d, 3d),
            GenerateTimeSeries(
                "data{type=\"some_nan\"}", 0, TimeSpan.FromSeconds(10), 2d, 0d, double.NaN),
            GenerateTimeSeries(
                "data{type=\"some_nan2\"}", 0, TimeSpan.FromSeconds(10), 2d, double.NaN, 1d),
            GenerateTimeSeries(
                "data{type=\"some_nan3\"}", 0, TimeSpan.FromSeconds(10), double.NaN, 0d, 1d),
            GenerateTimeSeries(
                "data{type=\"only_nan\"}", 0, TimeSpan.FromSeconds(10), double.NaN, double.NaN, double.NaN)
        };

        var engine = new PromQLEngine(
            new MochaPromQLParserParser(),
            new InMemoryPrometheusMetricsReader(series),
            Options.Create(new PromQLEngineOptions()));

        var result = await engine.QueryInstantAsync(
            "max_over_time(data[1m])",
            60,
            null,
            CancellationToken.None);

        result.Should().BeEquivalentTo(
            new VectorResult
            {
                new Sample
                {
                    Metric = new Labels { { "type", "numbers" } },
                    Point = new DoublePoint { TimestampUnixSec = 60, Value = 3 }
                },
                new Sample
                {
                    Metric = new Labels { { "type", "some_nan" } },
                    Point = new DoublePoint { TimestampUnixSec = 60, Value = 2 }
                },
                new Sample
                {
                    Metric = new Labels { { "type", "some_nan2" } },
                    Point = new DoublePoint { TimestampUnixSec = 60, Value = 2 }
                },
                new Sample
                {
                    Metric = new Labels { { "type", "some_nan3" } },
                    Point = new DoublePoint { TimestampUnixSec = 60, Value = 1 }
                },
                new Sample
                {
                    Metric = new Labels { { "type", "only_nan" } },
                    Point = new DoublePoint { TimestampUnixSec = 60, Value = double.NaN }
                }
            },
            opts => opts.RespectingRuntimeTypes()
                .Using<double>(ctx =>
                {
                    if (double.IsNaN(ctx.Expectation))
                        double.IsNaN(ctx.Subject).Should().BeTrue();
                    else
                        ctx.Subject.Should().BeApproximately(ctx.Expectation, 0.0001);
                })
                .WhenTypeIs<double>());

    }
}
