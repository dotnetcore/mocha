// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Mocha.Core.Models.Metrics;
using Mocha.Core.Storage.Prometheus;
using Mocha.Query.Prometheus.PromQL.Engine;
using Mocha.Query.Prometheus.PromQL.Values;

namespace Mocha.Query.Tests.Prometheus.Engine.Functions;

public class HistogramTests
{
    [Theory]
    [MemberData(nameof(TestCases))]
    public async Task Eval_Histogram_Quantile(EngineTestCase testCase)
    {
        var series = new[]
        {
            GenerateTimeSeries(
                "testhistogram_bucket{le=\"0.1\", start=\"positive\"}", TimeSpan.FromMinutes(5), 10, 0, 5),
            GenerateTimeSeries("testhistogram_bucket{le=\".2\", start=\"positive\"}",
                TimeSpan.FromMinutes(5), 10, 0, 7),
            GenerateTimeSeries("testhistogram_bucket{le=\"1e0\", start=\"positive\"}",
                TimeSpan.FromMinutes(5), 10, 0, 11),
            GenerateTimeSeries("testhistogram_bucket{le=\"+Inf\", start=\"positive\"}",
                TimeSpan.FromMinutes(5), 10, 0, 12),
            GenerateTimeSeries("testhistogram_bucket{le=\"-.2\", start=\"negative\"}",
                TimeSpan.FromMinutes(5), 10, 0, 1),
            GenerateTimeSeries("testhistogram_bucket{le=\"-0.1\", start=\"negative\"}",
                TimeSpan.FromMinutes(5), 10, 0, 2),
            GenerateTimeSeries("testhistogram_bucket{le=\"0.3\", start=\"negative\"}",
                TimeSpan.FromMinutes(5), 10, 0, 2),
            GenerateTimeSeries("testhistogram_bucket{le=\"+Inf\", start=\"negative\"}",
                TimeSpan.FromMinutes(5), 10, 0, 3),
            GenerateTimeSeries("request_duration_seconds_bucket{job=\"job1\", instance=\"ins1\", le=\"0.1\"}",
                TimeSpan.FromMinutes(5), 10, 0, 1),
            GenerateTimeSeries("request_duration_seconds_bucket{job=\"job1\", instance=\"ins1\", le=\"0.2\"}",
                TimeSpan.FromMinutes(5), 10, 0, 3),
            GenerateTimeSeries("request_duration_seconds_bucket{job=\"job1\", instance=\"ins1\", le=\"+Inf\"}",
                TimeSpan.FromMinutes(5), 10, 0, 4),
            GenerateTimeSeries("request_duration_seconds_bucket{job=\"job1\", instance=\"ins2\", le=\"0.1\"}",
                TimeSpan.FromMinutes(5), 10, 0, 2),
            GenerateTimeSeries("request_duration_seconds_bucket{job=\"job1\", instance=\"ins2\", le=\"0.2\"}",
                TimeSpan.FromMinutes(5), 10, 0, 5),
            GenerateTimeSeries("request_duration_seconds_bucket{job=\"job1\", instance=\"ins2\", le=\"+Inf\"}",
                TimeSpan.FromMinutes(5), 10, 0, 6),
            GenerateTimeSeries("request_duration_seconds_bucket{job=\"job2\", instance=\"ins1\", le=\"0.1\"}",
                TimeSpan.FromMinutes(5), 10, 0, 3),
            GenerateTimeSeries("request_duration_seconds_bucket{job=\"job2\", instance=\"ins1\", le=\"0.2\"}",
                TimeSpan.FromMinutes(5), 10, 0, 4),
            GenerateTimeSeries("request_duration_seconds_bucket{job=\"job2\", instance=\"ins1\", le=\"+Inf\"}",
                TimeSpan.FromMinutes(5), 10, 0, 6),
            GenerateTimeSeries("request_duration_seconds_bucket{job=\"job2\", instance=\"ins2\", le=\"0.1\"}",
                TimeSpan.FromMinutes(5), 10, 0, 4),
            GenerateTimeSeries("request_duration_seconds_bucket{job=\"job2\", instance=\"ins2\", le=\"0.2\"}",
                TimeSpan.FromMinutes(5), 10, 0, 7),
            GenerateTimeSeries("request_duration_seconds_bucket{job=\"job2\", instance=\"ins2\", le=\"+Inf\"}",
                TimeSpan.FromMinutes(5), 10, 0, 9),
            GenerateTimeSeries("mixed_bucket{job=\"job1\", instance=\"ins1\", le=\"0.1\"}",
                TimeSpan.FromMinutes(5), 10, 0, 1),
            GenerateTimeSeries("mixed_bucket{job=\"job1\", instance=\"ins1\", le=\"0.2\"}",
                TimeSpan.FromMinutes(5), 10, 0, 1),
            GenerateTimeSeries("mixed_bucket{job=\"job1\", instance=\"ins1\", le=\"2e-1\"}",
                TimeSpan.FromMinutes(5), 10, 0, 1),
            GenerateTimeSeries("mixed_bucket{job=\"job1\", instance=\"ins1\", le=\"2.0e-1\"}",
                TimeSpan.FromMinutes(5), 10, 0, 1),
            GenerateTimeSeries("mixed_bucket{job=\"job1\", instance=\"ins1\", le=\"+Inf\"}",
                TimeSpan.FromMinutes(5), 10, 0, 4),
            GenerateTimeSeries("mixed_bucket{job=\"job1\", instance=\"ins2\", le=\"+inf\"}",
                TimeSpan.FromMinutes(5), 10, 0, 0),
            GenerateTimeSeries("mixed_bucket{job=\"job1\", instance=\"ins2\", le=\"+Inf\"}",
                TimeSpan.FromMinutes(5), 10, 0, 0)
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
            // Quantile too low.
            new()
            {
                Query = "histogram_quantile(-0.1, testhistogram_bucket)",
                Result = new VectorResult
                {
                    new Sample
                    {
                        Metric = new Labels { { "start", "positive" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = double.NegativeInfinity }
                    },
                    new Sample
                    {
                        Metric = new Labels { { "start", "negative" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = double.NegativeInfinity }
                    }
                },
                StartTimestampUnixSec = 50 * 60
            },
            // Quantile too high.
            new()
            {
                Query = "histogram_quantile(1.01, testhistogram_bucket)",
                Result = new VectorResult
                {
                    new Sample
                    {
                        Metric = new Labels { { "start", "positive" } },
                        Point =
                            new DoublePoint { TimestampUnixSec = 50 * 60, Value = double.PositiveInfinity }
                    },
                    new Sample
                    {
                        Metric = new Labels { { "start", "negative" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = double.PositiveInfinity }
                    }
                },
                StartTimestampUnixSec = 50 * 60
            },
            // Quantile value in lowest bucket, which is positive.
            new()
            {
                Query = "histogram_quantile(0, testhistogram_bucket{start=\"positive\"})",
                Result = new VectorResult
                {
                    new Sample
                    {
                        Metric = new Labels { { "start", "positive" } },
                        Point = new DoublePoint { TimestampUnixSec = 50 * 60, Value = 0 }
                    }
                },
                StartTimestampUnixSec = 50 * 60
            }
        }.Select(x => new object[] { x });
}
