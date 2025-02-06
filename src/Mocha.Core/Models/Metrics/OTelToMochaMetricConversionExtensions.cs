// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Mocha.Core.Storage.Prometheus;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Metrics.V1;

namespace Mocha.Core.Models.Metrics;

public static partial class OTelToMochaMetricConversionExtensions
{
    public static IEnumerable<MochaMetricMetadata> ToMochaMetricMetadata(
        this Metric metric,
        Dictionary<string, string> resourceLabels)
    {
        var serviceName = resourceLabels.GetValueOrDefault("service_name", "unknown");
        var metricName = SanitizeMetricName(metric.Name);
        var unit = GetUnit(metric.Unit);
        if (!metricName.EndsWith(unit))
        {
            metricName += "_" + unit;
        }

        var metricMetadata = new MochaMetricMetadata
        {
            Metric = metricName,
            ServiceName = serviceName,
            Type = metric.DataCase switch
            {
                Metric.DataOneofCase.Gauge => MochaMetricType.Gauge,
                Metric.DataOneofCase.Sum => MochaMetricType.Sum,
                Metric.DataOneofCase.Histogram => MochaMetricType.Histogram,
                Metric.DataOneofCase.ExponentialHistogram => MochaMetricType.ExponentialHistogram,
                Metric.DataOneofCase.Summary => MochaMetricType.Summary,
                _ => throw new ArgumentOutOfRangeException()
            },
            Description = metric.Description,
            Unit = unit
        };
        MochaMetricMetadata[] metadataList = metric.DataCase switch
        {
            Metric.DataOneofCase.Gauge or Metric.DataOneofCase.Sum => [metricMetadata],
            Metric.DataOneofCase.Histogram or Metric.DataOneofCase.ExponentialHistogram =>
            [
                metricMetadata with { Metric = metricName + "_sum" },
                metricMetadata with { Metric = metricName + "_count", Unit = "1" },
                metricMetadata with { Metric = metricName + "_bucket" }
            ],
            Metric.DataOneofCase.Summary =>
            [
                metricMetadata with { Metric = metricName + "_sum" },
                metricMetadata with { Metric = metricName + "_count", Unit = "1" },
                metricMetadata with { Metric = metricName + "_quantile" }
            ],
            _ => throw new ArgumentOutOfRangeException()
        };
        return metadataList;
    }

    public static IEnumerable<MochaMetric> ToMochaMetric(this Metric metric, Dictionary<string, string> resourceLabels)
    {
        var mochaMetrics = metric.DataCase switch
        {
            Metric.DataOneofCase.Gauge => metric.ToMochaGaugeMetrics(resourceLabels),
            Metric.DataOneofCase.Sum => metric.ToMochaSumMetrics(resourceLabels),
            Metric.DataOneofCase.Histogram => metric.ToMochaHistogramMetrics(resourceLabels),
            Metric.DataOneofCase.ExponentialHistogram => metric.ToExponentialHistogramMetrics(resourceLabels),
            // TODO: Should we handle Summary?
            Metric.DataOneofCase.Summary => metric.ToMochaSummaryMetrics(resourceLabels),
            _ => throw new ArgumentOutOfRangeException()
        };

        foreach (var mochaMetric in mochaMetrics)
        {
            // add metric name to labels for Prometheus compatibility
            mochaMetric.Labels[Labels.MetricName] = mochaMetric.Name;
            yield return mochaMetric;
        }
    }

    public static Dictionary<string, string> ToMochaMetricLabels(this IEnumerable<KeyValue> attributes)
    {
        // TODO: Should we handle attributes with non-string values?
        return attributes.ToDictionary(
            attr => attr.Key.Replace('.', '_'),
            attr =>
            {
                var value = attr.Value;
                return value.ValueCase switch
                {
                    AnyValue.ValueOneofCase.StringValue => value.StringValue,
                    AnyValue.ValueOneofCase.BoolValue => value.BoolValue.ToString(),
                    AnyValue.ValueOneofCase.IntValue => value.IntValue.ToString(),
                    AnyValue.ValueOneofCase.DoubleValue => value.DoubleValue.ToString("R"),
                    _ => throw new ArgumentOutOfRangeException(nameof(value.ValueCase),
                        value.ValueCase,
                        "Unsupported attribute value case.")
                };
            });
    }

    private static IEnumerable<MochaMetric> ToMochaGaugeMetrics(
        this Metric metric,
        Dictionary<string, string> resourceLabels)
    {
        var metricName = SanitizeMetricName(metric.Name);
        var unit = GetUnit(metric.Unit);
        if (!metricName.EndsWith(unit))
        {
            metricName += "_" + unit;
        }

        var gauge = metric.Gauge;
        foreach (var dataPoint in gauge.DataPoints)
        {
            yield return new MochaMetric
            {
                Name = metricName,
                Type = MochaMetricType.Gauge,
                Description = metric.Description,
                Unit = unit,
                Labels = MergeLabels(resourceLabels, dataPoint.Attributes.ToMochaMetricLabels()),
                Value = dataPoint.ValueCase == NumberDataPoint.ValueOneofCase.AsDouble ? dataPoint.AsDouble :
                    dataPoint.ValueCase == NumberDataPoint.ValueOneofCase.AsInt ? dataPoint.AsInt :
                    throw new ArgumentOutOfRangeException(),
                TimestampUnixNano = dataPoint.TimeUnixNano
            };
        }
    }

    private static IEnumerable<MochaMetric> ToMochaSumMetrics(
        this Metric metric,
        Dictionary<string, string> resourceLabels)
    {
        var metricName = SanitizeMetricName(metric.Name);
        var unit = GetUnit(metric.Unit);
        if (!metricName.EndsWith(unit))
        {
            metricName += "_" + unit;
        }

        metricName += "_total";

        // TODO: Should we handle IsMonotonic and AggregationTemporality?
        var sum = metric.Sum;
        foreach (var dataPoint in sum.DataPoints)
        {
            yield return new MochaMetric
            {
                Name = metricName,
                Type = MochaMetricType.Sum,
                Description = metric.Description,
                Unit = unit,
                Labels = MergeLabels(resourceLabels, dataPoint.Attributes.ToMochaMetricLabels()),
                Value = dataPoint.ValueCase switch
                {
                    NumberDataPoint.ValueOneofCase.AsDouble => dataPoint.AsDouble,
                    NumberDataPoint.ValueOneofCase.AsInt => dataPoint.AsInt,
                    _ => throw new ArgumentOutOfRangeException()
                },
                TimestampUnixNano = dataPoint.TimeUnixNano
            };
        }
    }

    private static IEnumerable<MochaMetric> ToMochaHistogramMetrics(
        this Metric metric,
        Dictionary<string, string> resourceLabels)
    {
        var metricName = SanitizeMetricName(metric.Name);
        var unit = GetUnit(metric.Unit);
        if (!metricName.EndsWith(unit))
        {
            metricName += "_" + unit;
        }

        var histogram = metric.Histogram;
        foreach (var dataPoint in histogram.DataPoints)
        {
            var labels = MergeLabels(resourceLabels, dataPoint.Attributes.ToMochaMetricLabels());
            yield return new MochaMetric
            {
                Name = metricName + "_sum",
                Type = MochaMetricType.Histogram,
                Description = metric.Description,
                Unit = unit,
                Labels = labels,
                Value = dataPoint.Sum,
                TimestampUnixNano = dataPoint.TimeUnixNano
            };

            yield return new MochaMetric
            {
                Name = metricName + "_count",
                Type = MochaMetricType.Histogram,
                Description = metric.Description,
                Unit = unit,
                Labels = labels,
                Value = dataPoint.Count,
                TimestampUnixNano = dataPoint.TimeUnixNano
            };
            foreach (var (explicitBound, bucketCount) in BuildBuckets(dataPoint))
            {
                yield return new MochaMetric
                {
                    Name = metricName + "_bucket",
                    Type = MochaMetricType.Histogram,
                    Description = metric.Description,
                    Unit = unit,
                    Labels = MergeLabels(labels,
                        new Dictionary<string, string>
                        {
                            {
                                "le", double.IsPositiveInfinity(explicitBound) ? "+Inf" : explicitBound.ToString("F")
                            }
                        }),
                    Value = bucketCount,
                    TimestampUnixNano = dataPoint.TimeUnixNano
                };
            }
        }

        static Dictionary<double, ulong> BuildBuckets(HistogramDataPoint dataPoint)
        {
            var result = new Dictionary<double, ulong>();
            var bucketCounts = dataPoint.BucketCounts;
            var explicitBounds = dataPoint.ExplicitBounds;
            ulong count = 0;
            for (var i = 0; i < explicitBounds.Count; i++)
            {
                count += bucketCounts[i];
                result[explicitBounds[i]] = count;
            }

            count += bucketCounts[explicitBounds.Count];
            result.Add(double.PositiveInfinity, count);
            return result;
        }
    }

    private static IEnumerable<MochaMetric> ToExponentialHistogramMetrics(
        this Metric metric,
        Dictionary<string, string> resourceLabels)
    {
        var metricName = SanitizeMetricName(metric.Name);
        var unit = GetUnit(metric.Unit);
        if (!metricName.EndsWith(unit))
        {
            metricName += "_" + unit;
        }

        var expHistogram = metric.ExponentialHistogram;
        foreach (var dataPoint in expHistogram.DataPoints)
        {
            var labels = MergeLabels(resourceLabels, dataPoint.Attributes.ToMochaMetricLabels());
            yield return new MochaMetric
            {
                Name = metricName + "_sum",
                Type = MochaMetricType.ExponentialHistogram,
                Description = metric.Description,
                Unit = unit,
                Labels = labels,
                Value = dataPoint.Sum,
                TimestampUnixNano = dataPoint.TimeUnixNano
            };

            yield return new MochaMetric
            {
                Name = metricName + "_count",
                Type = MochaMetricType.ExponentialHistogram,
                Description = metric.Description,
                Unit = unit,
                Labels = labels,
                Value = dataPoint.Count,
                TimestampUnixNano = dataPoint.TimeUnixNano
            };
            foreach (var (explicitBound, bucketCount) in BuildBuckets(dataPoint))
            {
                yield return new MochaMetric
                {
                    Name = metricName + "_bucket",
                    Description = metric.Description,
                    Unit = unit,
                    Labels = MergeLabels(labels,
                        new Dictionary<string, string>
                        {
                            {
                                "le", double.IsPositiveInfinity(explicitBound) ? "+Inf" : explicitBound.ToString("F")
                            }
                        }),
                    Value = bucketCount,
                    TimestampUnixNano = dataPoint.TimeUnixNano
                };
            }
        }

        static Dictionary<double, ulong> BuildBuckets(ExponentialHistogramDataPoint dataPoint)
        {
            var result = new Dictionary<double, ulong>();
            var @base = Math.Pow(2.0, Math.Pow(2.0, -dataPoint.Scale));
            if (double.IsPositiveInfinity(@base))
            {
                return result;
            }

            double upperBound;
            ulong count = 0;

            for (var i = 0; i < dataPoint.Negative.BucketCounts.Count; i++)
            {
                upperBound = -Math.Pow(@base, dataPoint.Negative.Offset + i);
                if (double.IsNegativeInfinity(upperBound))
                {
                    return [];
                }

                count += dataPoint.Negative.BucketCounts[i];
                result[upperBound] = count;
            }

            for (var i = 0; i < dataPoint.Positive.BucketCounts.Count - 1; i++)
            {
                upperBound = Math.Pow(@base, dataPoint.Positive.Offset + i + 1);
                if (double.IsPositiveInfinity(upperBound))
                {
                    return [];
                }

                count += dataPoint.Positive.BucketCounts[i];
                result[upperBound] = count;
            }

            count += dataPoint.Positive.BucketCounts[^1];
            result[double.PositiveInfinity] = count;
            return result;
        }
    }

    private static IEnumerable<MochaMetric> ToMochaSummaryMetrics(
        this Metric metric,
        Dictionary<string, string> resourceLabels)
    {
        var metricName = SanitizeMetricName(metric.Name);
        var unit = GetUnit(metric.Unit);
        if (!metricName.EndsWith(unit))
        {
            metricName += "_" + unit;
        }

        var summary = metric.Summary;
        foreach (var dataPoint in summary.DataPoints)
        {
            var labels = MergeLabels(resourceLabels, dataPoint.Attributes.ToMochaMetricLabels());
            yield return new MochaMetric
            {
                Name = metricName + "_sum",
                Type = MochaMetricType.Summary,
                Description = metric.Description,
                Unit = unit,
                Labels = labels,
                Value = dataPoint.Sum,
                TimestampUnixNano = dataPoint.TimeUnixNano
            };

            yield return new MochaMetric
            {
                Name = metricName + "_count",
                Type = MochaMetricType.Summary,
                Description = metric.Description,
                Unit = unit,
                Labels = labels,
                Value = dataPoint.Count,
                TimestampUnixNano = dataPoint.TimeUnixNano
            };
            foreach (var quantileValue in dataPoint.QuantileValues)
            {
                yield return new MochaMetric
                {
                    Name = metricName + "_quantile",
                    Type = MochaMetricType.Summary,
                    Description = metric.Description,
                    Unit = unit,
                    Labels = MergeLabels(labels,
                        new Dictionary<string, string> { { "quantile", quantileValue.Quantile.ToString("F") } }),
                    Value = quantileValue.Value,
                    TimestampUnixNano = dataPoint.TimeUnixNano
                };
            }
        }
    }

    private static Dictionary<string, string> MergeLabels(
        Dictionary<string, string> resourceLabels,
        Dictionary<string, string> metricLabels)
    {
        var mergedLabels = new Dictionary<string, string>(resourceLabels);
        foreach (var label in metricLabels)
        {
            mergedLabels[label.Key] = label.Value;
        }

        return mergedLabels;
    }

    private static string SanitizeMetricName(string metricName)
    {
        var sanitizedName = metricName.Replace('.', '_');
        if (sanitizedName.EndsWith("_total"))
        {
            sanitizedName = sanitizedName[..^6];
        }

        return sanitizedName;
    }


    private static string GetUnit(string unit)
    {
        var updatedUnit = RemoveAnnotations(unit);

        if (TryProcessRateUnits(updatedUnit, out var updatedPerUnit))
        {
            return updatedPerUnit;
        }

        return MapUnit(updatedUnit);

        static string RemoveAnnotations(string unit) => RemoveAnnotationsRegex().Replace(unit, "");

        static bool TryProcessRateUnits(string updatedUnit, [NotNullWhen(true)] out string? updatedPerUnit)
        {
            updatedPerUnit = null;

            for (var i = 0; i < updatedUnit.Length; i++)
            {
                if (updatedUnit[i] != '/')
                {
                    continue;
                }

                // Only convert rate expressed units if it's a valid expression.
                if (i == updatedUnit.Length - 1)
                {
                    return false;
                }

                updatedPerUnit = MapUnit(updatedUnit.AsSpan(0, i)) + "_per_" +
                                 MapPerUnit(updatedUnit.AsSpan(i + 1, updatedUnit.Length - i - 1));
                return true;
            }

            return false;
        }

        static string MapUnit(ReadOnlySpan<char> unit)
        {
            return unit switch
            {
                // Time
                "d" => "days",
                "h" => "hours",
                "min" => "minutes",
                "s" => "seconds",
                "ms" => "milliseconds",
                "us" => "microseconds",
                "ns" => "nanoseconds",

                // Bytes
                "By" => "bytes",
                "KiBy" => "kibibytes",
                "MiBy" => "mebibytes",
                "GiBy" => "gibibytes",
                "TiBy" => "tibibytes",
                "KBy" => "kilobytes",
                "MBy" => "megabytes",
                "GBy" => "gigabytes",
                "TBy" => "terabytes",
                "B" => "bytes",
                "KB" => "kilobytes",
                "MB" => "megabytes",
                "GB" => "gigabytes",
                "TB" => "terabytes",

                // SI
                "m" => "meters",
                "V" => "volts",
                "A" => "amperes",
                "J" => "joules",
                "W" => "watts",
                "g" => "grams",

                // Misc
                "Cel" => "celsius",
                "Hz" => "hertz",
                "1" => string.Empty,
                "%" => "percent",
                "$" => "dollars",
                _ => unit.ToString()
            };
        }

        static string MapPerUnit(ReadOnlySpan<char> perUnit)
        {
            return perUnit switch
            {
                "s" => "second",
                "m" => "minute",
                "h" => "hour",
                "d" => "day",
                "w" => "week",
                "mo" => "month",
                "y" => "year",
                _ => perUnit.ToString()
            };
        }
    }

    [GeneratedRegex(@"\{[^{}]*\}")]
    private static partial Regex RemoveAnnotationsRegex();
}
