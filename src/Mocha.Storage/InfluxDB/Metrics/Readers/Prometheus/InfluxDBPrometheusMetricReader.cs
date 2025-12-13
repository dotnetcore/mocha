// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Text;
using InfluxDB.Client;
using Microsoft.Extensions.Options;
using Mocha.Core.Models.Metrics;
using Mocha.Core.Storage.Prometheus;
using Mocha.Core.Storage.Prometheus.Metrics;

namespace Mocha.Storage.InfluxDB.Metrics.Readers.Prometheus;

public class InfluxDbPrometheusMetricReader(
    IInfluxDBClient influxDbClient,
    IOptions<InfluxDBOptions> options)
    : IPrometheusMetricReader
{
    private readonly IQueryApi _reader = influxDbClient.GetQueryApi();
    private readonly InfluxDBOptions _options = options.Value;

    public async Task<IEnumerable<TimeSeries>> GetTimeSeriesAsync(
        TimeSeriesQueryParameters query,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var metricName = string.Empty;
        var labelMatchers = new List<LabelMatcher>();

        foreach (var labelMatcher in query.LabelMatchers)
        {
            if (labelMatcher is { Name: Labels.MetricName, Type: LabelMatcherType.Equal })
            {
                metricName = labelMatcher.Value;
                continue;
            }

            labelMatchers.Add(labelMatcher);
        }

        var sb = new StringBuilder();
        sb.AppendLine(
            $"""
             from(bucket: "{_options.Bucket}")
              |> range(start: {query.StartTimestampUnixSec}, stop: {query.EndTimestampUnixSec})
             """);

        if (!string.IsNullOrEmpty(metricName))
        {
            sb.AppendLine($" |> filter(fn: (r) => r._measurement == \"{metricName}\")");
        }

        if (labelMatchers.Count > 1)
        {
            sb.Append(" |> filter(fn: (r) => ");
            sb.Append(BuildLabelMatchersQuery(labelMatchers));
            sb.AppendLine(")");
        }

        sb.AppendLine(" |> sort(columns: [\"_time\"])");
        sb.AppendLine($" |> limit(n: {query.Limit})");

        var result = await _reader.QueryAsync(sb.ToString(), _options.Org, cancellationToken);

        if (result.Count == 0)
        {
            return [];
        }

        var grouped = new Dictionary<Labels, List<TimeSeriesSample>>();

        foreach (var table in result)
        {
            var columns = table.Columns.Where(c => !c.Label.StartsWith('_')).Select(c => c.Label).ToList();
            foreach (var record in table.Records)
            {
                var labels = new Labels { [Labels.MetricName] = metricName };
                var value = Convert.ToDouble(record.GetValue());
                var timestamp = record.GetTime()!.Value.ToUnixTimeSeconds();
                foreach (var column in columns)
                {
                    labels.Add(column, record.GetValueByKey(column).ToString()!);
                }

                if (!grouped.TryGetValue(labels, out var samples))
                {
                    samples = [];
                    grouped.Add(labels, samples);
                }

                samples.Add(new TimeSeriesSample { Value = value, TimestampUnixSec = timestamp });
            }
        }

        var timeSeriess = grouped.Select(kvp => new TimeSeries { Labels = kvp.Key, Samples = kvp.Value }).ToList();
        return timeSeriess;
    }

    public async Task<IEnumerable<string>> GetLabelNamesAsync(LabelNamesQueryParameters query)
    {
        var metricName = string.Empty;
        var labelMatchers = new List<LabelMatcher>();

        foreach (var labelMatcher in query.LabelMatchers)
        {
            if (labelMatcher is { Name: Labels.MetricName, Type: LabelMatcherType.Equal })
            {
                metricName = labelMatcher.Value;
                continue;
            }

            labelMatchers.Add(labelMatcher);
        }

        var sb = new StringBuilder();
        sb.AppendLine($"from(bucket: \"{_options.Bucket}\")");
        if (query is { StartTimestampUnixSec: not null, EndTimestampUnixSec: not null })
        {
            sb.AppendLine($" |> range(start: {query.StartTimestampUnixSec}, stop: {query.EndTimestampUnixSec})");
        }

        if (!string.IsNullOrEmpty(metricName))
        {
            sb.AppendLine($" |> filter(fn: (r) => r._measurement == \"{metricName}\")");
        }

        if (labelMatchers.Count > 1)
        {
            sb.Append(" |> filter(fn: (r) => ");
            sb.Append(BuildLabelMatchersQuery(labelMatchers));
            sb.AppendLine(")");
        }

        sb.AppendLine("""
                       |> keys()
                       |> keep(columns: ["_value"])
                       |> distinct()
                       |> filter(fn: (r) => r._value !~ /^_/)
                       |> sort()
                      """);

        var result = await _reader.QueryAsync(sb.ToString(), _options.Org);

        if (result.Count == 0)
        {
            return [];
        }

        var labelNames = result.SelectMany(r => r.Records.Select(record => record.GetValue().ToString()!))
            .ToList();
        return labelNames;
    }


    public async Task<IEnumerable<string>> GetLabelValuesAsync(LabelValuesQueryParameters query)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"from(bucket: \"{_options.Bucket}\")");
        if (query is { StartTimestampUnixSec: not null, EndTimestampUnixSec: not null })
        {
            sb.AppendLine($" |> range(start: {query.StartTimestampUnixSec}, stop: {query.EndTimestampUnixSec})");
        }

        sb.AppendLine($"""
                        |> filter(fn: (r) => r.{query.LabelName}!= "")
                        |> keep(columns: ["{query.LabelName}"])
                        |> distinct(column: "{query.LabelName}")
                        |> sort(columns: ["{query.LabelName}"])
                        |> limit(n: {query.Limit})
                       """);

        var result = await _reader.QueryAsync(sb.ToString(), _options.Org);

        if (result.Count == 0)
        {
            return [];
        }

        var labelValues = result.SelectMany(r => r.Records.Select(record => record.GetValue().ToString()!)).ToList();

        return labelValues;
    }

    private static string BuildLabelMatchersQuery(IEnumerable<LabelMatcher> labelMatchers)
    {
        var labelMatcherQLs = new List<string>();

        foreach (var (name, labelValue, labelMatcherType) in labelMatchers)
        {
            if (name == Labels.MetricName)
            {
                continue;
            }

            switch (labelMatcherType)
            {
                case LabelMatcherType.Equal:
                    labelMatcherQLs.Add(labelValue == string.Empty
                        ? $"(r.{name} == \"\" or not exists r.{name})"
                        : $"r.{name} == \"{labelValue}\"");
                    break;
                case LabelMatcherType.NotEqual:
                    labelMatcherQLs.Add($"r.{name} != \"{labelValue}\"");
                    break;
                case LabelMatcherType.RegexMatch:
                    labelMatcherQLs.Add($"r.{name} =~ /{labelValue}/");
                    break;
                case LabelMatcherType.RegexNotMatch:
                    labelMatcherQLs.Add($"r.{name} !~ /{labelValue}/ ");
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected label matcher type: {labelMatcherType}");
            }
        }

        var query = string.Join(" and ", labelMatcherQLs);
        return query;
    }
}
