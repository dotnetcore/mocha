// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mocha.Core.Models.Metrics;
using Mocha.Core.Storage;
using Mocha.Core.Storage.Prometheus;
using Mocha.Core.Storage.Prometheus.Metrics;
using Mocha.Storage.LiteDB.Metrics;
using Mocha.Storage.LiteDB.Metrics.Readers.Prometheus;
using Mocha.Storage.LiteDB.Metrics.Writers;

namespace Mocha.Storage.Tests.LiteDB;

public class LiteDBMetricsTests : IDisposable
{
    private readonly TempDatabasePath _tempDatabasePath;
    private readonly ServiceProvider _serviceProvider;

    private readonly ITelemetryDataWriter<MochaMetric> _writer;
    private readonly IPrometheusMetricReader _reader;


    public LiteDBMetricsTests()
    {
        _tempDatabasePath = TempDatabasePath.Create();
        var services = new ServiceCollection();

        services.AddStorage()
           .WithMetrics(options =>
           {
               options.UseLiteDB(liteDbOptions =>
               {
                   liteDbOptions.DatabasePath = _tempDatabasePath.Path;
               });
           });

        _serviceProvider = services.BuildServiceProvider();

        _writer = _serviceProvider.GetRequiredService<ITelemetryDataWriter<MochaMetric>>();
        _reader = _serviceProvider.GetRequiredService<IPrometheusMetricReader>();
    }

    [Fact]
    public async Task Reader_Equal_Matcher_Works()
    {
        var metric = new MochaMetric
        {
            Name = "http_requests_total",
            Labels = new Labels { { "http_route", "/api/values" }, { "method", "GET" } },
            TimestampUnixNano = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000,
            Value = 1.0,
            Description = "Total number of HTTP requests",
            Unit = "requests"
        };

        await _writer.WriteAsync([metric]);

        var queryParameters = new TimeSeriesQueryParameters
        {
            LabelMatchers =
            [
                new LabelMatcher(
                    Name: "__name__",
                    Value: "http_requests_total",
                    Type: LabelMatcherType.Equal),
                new LabelMatcher(
                    Name: "http_route",
                    Value: "/api/values",
                    Type: LabelMatcherType.Equal),
                new LabelMatcher(
                    Name: "method",
                    Value: "GET",
                    Type: LabelMatcherType.Equal)
            ],
            StartTimestampUnixSec = DateTimeOffset.UtcNow.AddMinutes(-5)
                .ToUnixTimeSeconds(),
            EndTimestampUnixSec = DateTimeOffset.UtcNow.AddMinutes(5)
                .ToUnixTimeSeconds(),
            Limit = 1000
        };

        var timeSeries = await _reader.GetTimeSeriesAsync(queryParameters, CancellationToken.None);

        timeSeries.Should().BeEquivalentTo([
            new TimeSeries
            {
                Labels = new Labels { { "http_route", "/api/values" }, { "method", "GET" } },
                Samples =
                [
                    new TimeSeriesSample
                    {
                        TimestampUnixSec = (long)(metric.TimestampUnixNano / 1_000_000_000), Value = metric.Value
                    }
                ]
            }
        ]);
    }

    [Fact]
    public async Task Reader_NotEqual_Matcher_Works()
    {
        var metric1 = new MochaMetric
        {
            Name = "http_requests_total",
            Labels = new Labels { { "http_route", "/api/values" }, { "method", "GET" } },
            TimestampUnixNano = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000,
            Value = 1.0,
            Description = "Total number of HTTP requests",
            Unit = "requests"
        };

        var metric2 = new MochaMetric
        {
            Name = "http_requests_total",
            Labels = new Labels { { "http_route", "/api/values" }, { "method", "POST" } },
            TimestampUnixNano = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000,
            Value = 2.0,
            Description = "Total number of HTTP requests",
            Unit = "requests"
        };

        await _writer.WriteAsync([metric1, metric2]);

        var queryParameters = new TimeSeriesQueryParameters
        {
            LabelMatchers =
            [
                new LabelMatcher(
                    Name: "http_route",
                    Value: "/api/values",
                    Type: LabelMatcherType.Equal),
                new LabelMatcher(
                    Name: "method",
                    Value: "POST",
                    Type: LabelMatcherType.NotEqual)
            ],
            StartTimestampUnixSec = DateTimeOffset.UtcNow.AddMinutes(-5)
                .ToUnixTimeSeconds(),
            EndTimestampUnixSec = DateTimeOffset.UtcNow.AddMinutes(5)
                .ToUnixTimeSeconds(),
            Limit = 1000
        };

        var timeSeries = await _reader.GetTimeSeriesAsync(queryParameters, CancellationToken.None);

        timeSeries.Should().BeEquivalentTo([
            new TimeSeries
            {
                Labels = new Labels { { "http_route", "/api/values" }, { "method", "GET" } },
                Samples =
                [
                    new TimeSeriesSample
                    {
                        TimestampUnixSec = (long)(metric1.TimestampUnixNano / 1_000_000_000), Value = metric1.Value
                    }
                ]
            }
        ]);
    }

    [Fact]
    public async Task Reader_RegexMatch_Matcher_Works()
    {
        var metric1 = new MochaMetric
        {
            Name = "cpu_usage_seconds_total",
            Labels = new Labels { { "instance", "server1" }, { "job", "database" } },
            TimestampUnixNano = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000,
            Value = 120.5,
            Description = "Total CPU usage in seconds",
            Unit = "seconds"
        };

        var metric2 = new MochaMetric
        {
            Name = "cpu_usage_seconds_total",
            Labels = new Labels { { "instance", "server2" }, { "job", "webserver" } },
            TimestampUnixNano = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000,
            Value = 95.3,
            Description = "Total CPU usage in seconds",
            Unit = "seconds"
        };

        await _writer.WriteAsync([metric1, metric2]);

        var queryParameters = new TimeSeriesQueryParameters
        {
            LabelMatchers =
            [
                new LabelMatcher(
                    Name: "job",
                    Value: "data.*",
                    Type: LabelMatcherType.RegexMatch)
            ],
            StartTimestampUnixSec = DateTimeOffset.UtcNow.AddMinutes(-5)
                .ToUnixTimeSeconds(),
            EndTimestampUnixSec = DateTimeOffset.UtcNow.AddMinutes(5)
                .ToUnixTimeSeconds(),
            Limit = 1000
        };

        var timeSeries = await _reader.GetTimeSeriesAsync(queryParameters, CancellationToken.None);

        timeSeries.Should().BeEquivalentTo([
            new TimeSeries
            {
                Labels = new Labels { { "instance", "server1" }, { "job", "database" } },
                Samples =
                [
                    new TimeSeriesSample
                    {
                        TimestampUnixSec = (long)(metric1.TimestampUnixNano / 1_000_000_000), Value = metric1.Value
                    }
                ]
            }
        ]);
    }

    [Fact]
    public async Task Reader_RegexNotMatch_Matcher_Works()
    {
        var metric1 = new MochaMetric
        {
            Name = "disk_io_seconds_total",
            Labels = new Labels { { "device", "sda1" }, { "operation", "read" } },
            TimestampUnixNano = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000,
            Value = 300.0,
            Description = "Total disk I/O in seconds",
            Unit = "seconds"
        };

        var metric2 = new MochaMetric
        {
            Name = "disk_io_seconds_total",
            Labels = new Labels { { "device", "sdb1" }, { "operation", "write" } },
            TimestampUnixNano = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000,
            Value = 450.0,
            Description = "Total disk I/O in seconds",
            Unit = "seconds"
        };

        await _writer.WriteAsync([metric1, metric2]);

        var queryParameters = new TimeSeriesQueryParameters
        {
            LabelMatchers =
            [
                new LabelMatcher(
                    Name: "operation",
                    Value: "read.*",
                    Type: LabelMatcherType.RegexNotMatch)
            ],
            StartTimestampUnixSec = DateTimeOffset.UtcNow.AddMinutes(-5)
                .ToUnixTimeSeconds(),
            EndTimestampUnixSec = DateTimeOffset.UtcNow.AddMinutes(5)
                .ToUnixTimeSeconds(),
            Limit = 1000
        };

        var timeSeries = await _reader.GetTimeSeriesAsync(queryParameters, CancellationToken.None);

        timeSeries.Should().BeEquivalentTo([
            new TimeSeries
            {
                Labels = new Labels { { "device", "sdb1" }, { "operation", "write" } },
                Samples =
                [
                    new TimeSeriesSample
                    {
                        TimestampUnixSec = (long)(metric2.TimestampUnixNano / 1_000_000_000), Value = metric2.Value
                    }
                ]
            }
        ]);
    }

    [Fact]
    public async Task Reader_NoResults_Returns_Empty()
    {
        var queryParameters = new TimeSeriesQueryParameters
        {
            LabelMatchers =
            [
                new LabelMatcher(
                    Name: "non_existent_label",
                    Value: "some_value",
                    Type: LabelMatcherType.Equal)
            ],
            StartTimestampUnixSec = DateTimeOffset.UtcNow.AddMinutes(-5)
                .ToUnixTimeSeconds(),
            EndTimestampUnixSec = DateTimeOffset.UtcNow.AddMinutes(5)
                .ToUnixTimeSeconds(),
            Limit = 1000
        };
        var timeSeries = await _reader.GetTimeSeriesAsync(queryParameters, CancellationToken.None);
        timeSeries.Should().BeEmpty();
    }

    [Fact]
    public async Task Reader_GetLabelNames()
    {
        var metric = new MochaMetric
        {
            Name = "http_requests_total",
            Labels = new Labels { { "http_route", "/api/values" }, { "method", "GET" } },
            TimestampUnixNano = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000,
            Value = 1.0,
            Description = "Total number of HTTP requests",
            Unit = "requests"
        };

        await _writer.WriteAsync([metric]);

        var labelNames = await _reader.GetLabelNamesAsync(new LabelNamesQueryParameters
        {
            StartTimestampUnixSec = DateTimeOffset.UtcNow.AddMinutes(-5)
                .ToUnixTimeSeconds(),
            EndTimestampUnixSec = DateTimeOffset.UtcNow.AddMinutes(5)
                .ToUnixTimeSeconds(),
            LabelMatchers =
            [
                new LabelMatcher(
                    Name: "http_route",
                    Value: "/api/values",
                    Type: LabelMatcherType.Equal),
                new LabelMatcher(
                    Name: "method",
                    Value: "GET",
                    Type: LabelMatcherType.Equal)
            ]
        });

        labelNames.Should().BeEquivalentTo("http_route", "method");
    }

    [Fact]
    public async Task Reader_GetLabelValues()
    {
        var metric = new MochaMetric
        {
            Name = "http_requests_total",
            Labels = new Labels { { "http_route", "/api/values" }, { "method", "GET" } },
            TimestampUnixNano = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000,
            Value = 1.0,
            Description = "Total number of HTTP requests",
            Unit = "requests"
        };

        await _writer.WriteAsync([metric]);
        var labelValues = await _reader.GetLabelValuesAsync(new LabelValuesQueryParameters
        {
            LabelName = "http_route",
            StartTimestampUnixSec = DateTimeOffset.UtcNow.AddMinutes(-5)
                .ToUnixTimeSeconds(),
            EndTimestampUnixSec = DateTimeOffset.UtcNow.AddMinutes(5)
                .ToUnixTimeSeconds(),
        });
        labelValues.Should().BeEquivalentTo("/api/values");
    }

    public void Dispose()
    {
        _tempDatabasePath.Dispose();
        _serviceProvider.Dispose();
    }
}
