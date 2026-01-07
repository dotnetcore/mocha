// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using FluentAssertions;

namespace Mocha.Core.Tests.Conversions;

using System.Collections.Generic;
using System.Linq;
using Mocha.Core.Models.Metrics;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Metrics.V1;
using Xunit;

public class OTelToMochaMetricConversionExtensionsTests
{
    [Fact]
    public void ToMochaMetricLabels_ShouldHandleAllLabelValueTypes()
    {
        var attributes = new List<KeyValue>
        {
            new() { Key = "string.label", Value = new AnyValue { StringValue = "value" } },
            new() { Key = "bool.label", Value = new AnyValue { BoolValue = true } },
            new() { Key = "int.label", Value = new AnyValue { IntValue = 123 } },
            new() { Key = "double.label", Value = new AnyValue { DoubleValue = 1.23 } },
            new() { Key = "empty.string", Value = new AnyValue { StringValue = "" } }
        };

        var labels = attributes.ToMochaMetricLabels();

        labels.Should().HaveCount(4);

        labels["string_label"].Should().Be("value");
        labels["bool_label"].Should().Be("True");
        labels["int_label"].Should().Be("123");
        labels["double_label"].Should().Be("1.23");

        labels.Should().NotContainKey("empty_string");
    }

    [Fact]
    public void ToMochaGaugeMetricMetadata()
    {
        var metric = new Metric { Name = "cpu.usage", Unit = "1", Description = "cpu usage", Gauge = new Gauge() };

        var resourceLabels = new Dictionary<string, string> { { "service_name", "test-service" } };

        var result = metric.ToMochaMetricMetadata(resourceLabels).ToList();

        result.Should().HaveCount(1);
        result[0].Metric.Should().Be("cpu_usage");
        result[0].Type.Should().Be(MochaMetricType.Gauge);
        result[0].ServiceName.Should().Be("test-service");
        result[0].Unit.Should().Be(string.Empty);
    }

    [Fact]
    public void ToMochaSumMetricMetadata()
    {
        var metric = new Metric { Name = "http.requests", Unit = "1", Sum = new Sum() };

        var resourceLabels = new Dictionary<string, string> { { "service_name", "test-service" } };

        var result = metric.ToMochaMetricMetadata(resourceLabels).ToList();

        result.Should().HaveCount(1);
        result[0].Metric.Should().Be("http_requests");
        result[0].Type.Should().Be(MochaMetricType.Sum);
    }

    [Fact]
    public void ToMochaHistogramMetricMetadata()
    {
        var metric = new Metric { Name = "request.duration", Unit = "ms", Histogram = new Histogram() };

        var result = metric.ToMochaMetricMetadata(
                new Dictionary<string, string> { { "service_name", "test-service" } })
            .ToList();

        result.Should().HaveCount(3);

        result.Select(m => m.Metric).Should().BeEquivalentTo(
            "request_duration_milliseconds_sum",
            "request_duration_milliseconds_count",
            "request_duration_milliseconds_bucket"
        );

        result.All(m => m.Type == MochaMetricType.Histogram).Should().BeTrue();
    }

    [Fact]
    public void ToMochaExponentialHistogramMetricMetadata()
    {
        var metric = new Metric
        {
            Name = "payload.size",
            Unit = "By",
            ExponentialHistogram = new ExponentialHistogram()
        };

        var result = metric.ToMochaMetricMetadata(
                new Dictionary<string, string> { { "service_name", "test-service" } })
            .ToList();

        result.Should().HaveCount(3);
        result.Should().AllSatisfy(m =>
            m.Type.Should().Be(MochaMetricType.ExponentialHistogram));
    }

    [Fact]
    public void ToMochaSummaryMetricMetadata()
    {
        var metric = new Metric { Name = "response.time", Unit = "ms", Summary = new Summary() };

        var result = metric.ToMochaMetricMetadata(
                new Dictionary<string, string> { { "service_name", "test-service" } })
            .ToList();

        result.Should().HaveCount(3);

        result.Select(m => m.Metric).Should().BeEquivalentTo(
            "response_time_milliseconds_sum",
            "response_time_milliseconds_count",
            "response_time_milliseconds_quantile"
        );

        result.All(m => m.Type == MochaMetricType.Summary).Should().BeTrue();
    }

    [Fact]
    public void ToMochaGaugeMetric()
    {
        var resourceLabels = new Labels(new Dictionary<string, string>
        {
            { "service_name", "test-service" }, { "env", "test" }
        });

        var metric = new Metric
        {
            Name = "cpu.usage",
            Unit = "1",
            Description = "cpu usage",
            Gauge = new Gauge
            {
                DataPoints =
                {
                    new NumberDataPoint
                    {
                        AsDouble = 0.5,
                        TimeUnixNano = 123,
                        Attributes =
                        {
                            new KeyValue
                            {
                                Key = "core", Value = new AnyValue { StringValue = "0" }
                            }
                        }
                    }
                }
            }
        };

        var result = metric.ToMochaMetric(resourceLabels).ToList();

        result.Should().HaveCount(1);
        result[0].Type.Should().Be(MochaMetricType.Gauge);
        result[0].Name.Should().Be("cpu_usage");
        result[0].Value.Should().Be(0.5);
    }

    [Fact]
    public void ToMochaSumMetric()
    {
        var resourceLabels = new Labels(new Dictionary<string, string> { { "service_name", "test-service" } });

        var metric = new Metric
        {
            Name = "http.requests",
            Unit = "1",
            Sum = new Sum { DataPoints = { new NumberDataPoint { AsInt = 10, TimeUnixNano = 456 } } }
        };

        var result = metric.ToMochaMetric(resourceLabels).ToList();

        result.Should().HaveCount(1);
        result[0].Type.Should().Be(MochaMetricType.Sum);
        result[0].Name.Should().Be("http_requests_total");
        result[0].Value.Should().Be(10);
    }

    [Fact]
    public void ToMochaHistogramMetric()
    {
        var resourceLabels = new Labels(new Dictionary<string, string> { { "service_name", "test-service" } });

        var metric = new Metric
        {
            Name = "request.duration",
            Unit = "ms",
            Histogram = new Histogram
            {
                DataPoints =
                {
                    new HistogramDataPoint
                    {
                        Sum = 100, Count = 5, ExplicitBounds = { 10, 50 }, BucketCounts = { 1, 2, 2 }
                    }
                }
            }
        };

        var result = metric.ToMochaMetric(resourceLabels).ToList();

        result.Should().Contain(m => m.Name.EndsWith("_sum"));
        result.Should().Contain(m => m.Name.EndsWith("_count"));
        result.Should().Contain(m => m.Name.EndsWith("_bucket"));
    }

    [Fact]
    public void ToMochaExponentialHistogramMetric()
    {
        var resourceLabels = new Labels(new Dictionary<string, string> { { "service_name", "test-service" } });

        var metric = new Metric
        {
            Name = "payload.size",
            Unit = "By",
            ExponentialHistogram = new ExponentialHistogram
            {
                DataPoints =
                {
                    new ExponentialHistogramDataPoint
                    {
                        Sum = 1024,
                        Count = 3,
                        Scale = 1,
                        Positive = new ExponentialHistogramDataPoint.Types.Buckets
                        {
                            Offset = 0, BucketCounts = { 1, 2 }
                        },
                        Negative = new ExponentialHistogramDataPoint.Types.Buckets()
                    }
                }
            }
        };

        var result = metric.ToMochaMetric(resourceLabels).ToList();

        result.Should().Contain(m => m.Name.EndsWith("_sum"));
        result.Should().Contain(m => m.Name.EndsWith("_count"));
        result.Should().Contain(m => m.Name.EndsWith("_bucket"));
    }

    [Fact]
    public void ToMochaSummaryMetric()
    {
        var resourceLabels = new Labels(new Dictionary<string, string> { { "service_name", "test-service" } });

        var metric = new Metric
        {
            Name = "response.time",
            Unit = "ms",
            Summary = new Summary
            {
                DataPoints =
                {
                    new SummaryDataPoint
                    {
                        Sum = 200,
                        Count = 4,
                        QuantileValues =
                        {
                            new SummaryDataPoint.Types.ValueAtQuantile
                            {
                                Quantile = 0.5, Value = 40
                            },
                            new SummaryDataPoint.Types.ValueAtQuantile
                            {
                                Quantile = 0.9, Value = 80
                            }
                        }
                    }
                }
            }
        };

        var result = metric.ToMochaMetric(resourceLabels).ToList();

        result.Should().Contain(m => m.Name.EndsWith("_sum"));
        result.Should().Contain(m => m.Name.EndsWith("_count"));
        result.Should().Contain(m => m.Name.EndsWith("_quantile"));
    }
}
