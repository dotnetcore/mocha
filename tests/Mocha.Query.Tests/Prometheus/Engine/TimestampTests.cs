// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Mocha.Core.Storage.Prometheus;
using Mocha.Core.Storage.Prometheus.Metrics;
using Mocha.Query.Prometheus.PromQL.Engine;
using Mocha.Query.Prometheus.PromQL.Values;

namespace Mocha.Query.Tests.Prometheus.Engine;

public class TimestampTests
{
    [Theory]
    [MemberData(nameof(TestCases))]
    public async Task Eval_Timestamps(EngineTestCase testCase)
    {
        var mockReader = new Mock<IPrometheusMetricReader>();
        var mockOptions = new Mock<IOptions<PromQLEngineOptions>>();

        mockReader.Setup(x => x.GetTimeSeriesAsync(It.IsAny<TimeSeriesQueryParameters>()))
            .ReturnsAsync([
                new TimeSeries(new Labels { ["__name__"] = "metric" }, [
                    new TimeSeriesSample { TimestampUnixSec = 0, Value = 1 },
                    new TimeSeriesSample { TimestampUnixSec = 10, Value = 2 }
                ])
            ]);

        mockOptions.SetupGet(x => x.Value).Returns(new PromQLEngineOptions
        {
            DefaultEvaluationInterval = TimeSpan.FromSeconds(15),
            MaxSamplesPerQuery = 50000000,
        });

        var engine = new PromQLEngine(new MochaPromQLParserParser(), mockReader.Object, mockOptions.Object);

        if (testCase.Interval == TimeSpan.Zero)
        {
            var result =
                await engine.QueryInstantAsync(
                    testCase.Query,
                    testCase.StartTimestampUnixSec,
                    CancellationToken.None);

            result.Should().BeEquivalentTo(testCase.Result,
                options => options.RespectingRuntimeTypes());
        }
        else
        {
            var result =
                await engine.QueryRangeAsync(
                    testCase.Query,
                    testCase.StartTimestampUnixSec,
                    testCase.EndTimestampUnixSec,
                    testCase.Interval,
                    CancellationToken.None);

            result.Should().BeEquivalentTo((MatrixResult)testCase.Result,
                options => options.RespectingRuntimeTypes());
        }
    }

    public static IEnumerable<object[]> TestCases = new EngineTestCase[]
    {
        #region Instant queries

        new()
        {
            Query = "1", Result = new ScalarResult { TimestampUnixSec = 1, Value = 1 }, StartTimestampUnixSec = 1,
        },
        new EngineTestCase
        {
            Query = "metric",
            Result = new VectorResult
            {
                new Sample
                {
                    Point = new DoublePoint { TimestampUnixSec = 1, Value = 1 },
                    Metric = new Labels { ["__name__"] = "metric" }
                },
            },
            StartTimestampUnixSec = 1,
        },
        new EngineTestCase
        {
            Query = "metric[20s]",
            Result = new MatrixResult
            {
                new Series
                {
                    Points =
                    [
                        new DoublePoint { TimestampUnixSec = 0, Value = 1 },
                        new DoublePoint { TimestampUnixSec = 10, Value = 2 }
                    ],
                    Metric = new Labels { ["__name__"] = "metric" }
                },
            },
            StartTimestampUnixSec = 10,
        },
        new()
        {
            Query = "metric[20s]",
            Result = new MatrixResult
            {
                new Series
                {
                    Points =
                    [
                        new DoublePoint { TimestampUnixSec = 0, Value = 1 },
                        new DoublePoint { TimestampUnixSec = 10, Value = 2 }
                    ],
                    Metric = new Labels { ["__name__"] = "metric" }
                },
            },
            StartTimestampUnixSec = 10,
        },

        #endregion

        #region Range queries

        new()
        {
            Query = "1",
            Result = new MatrixResult
            {
                new Series
                {
                    Points =
                    [
                        new DoublePoint { TimestampUnixSec = 0, Value = 1 },
                        new DoublePoint { TimestampUnixSec = 1, Value = 1 },
                        new DoublePoint { TimestampUnixSec = 2, Value = 1 }
                    ],
                    Metric = new Labels()
                }
            },
            StartTimestampUnixSec = 0,
            EndTimestampUnixSec = 2,
            Interval = TimeSpan.FromSeconds(1),
        },
        new()
        {
            Query = "metric",
            Result = new MatrixResult
            {
                new Series
                {
                    Points =
                    [
                        new DoublePoint { TimestampUnixSec = 0, Value = 1 },
                        new DoublePoint { TimestampUnixSec = 1, Value = 1 },
                        new DoublePoint { TimestampUnixSec = 2, Value = 1 }
                    ],
                    Metric = new Labels { ["__name__"] = "metric" }
                }
            },
            StartTimestampUnixSec = 0,
            EndTimestampUnixSec = 2,
            Interval = TimeSpan.FromSeconds(1),
        },
        new()
        {
            Query = "metric",
            Result = new MatrixResult
            {
                new Series
                {
                    Points =
                    [
                        new DoublePoint { TimestampUnixSec = 0, Value = 1 },
                        new DoublePoint { TimestampUnixSec = 5, Value = 1 },
                        new DoublePoint { TimestampUnixSec = 10, Value = 2 }
                    ],
                    Metric = new Labels { ["__name__"] = "metric" }
                },
            },
            StartTimestampUnixSec = 0,
            EndTimestampUnixSec = 10,
            Interval = TimeSpan.FromSeconds(5),
        },

        #endregion

    }.Select(x => new object[] { x });
}
