// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Mocha.Core.Models.Metrics;
using Mocha.Core.Storage.Prometheus;
using Mocha.Core.Storage.Prometheus.Metrics;
using Mocha.Storage.LiteDB.Metrics.Models;

namespace Mocha.Storage.LiteDB.Metrics.Readers.Prometheus;

internal class LiteDBPrometheusMetricsReader(
    ILiteDBCollectionAccessor<LiteDBMetric> collectionAccessor)
    : IPrometheusMetricsReader
{
    public Task<IEnumerable<TimeSeries>> GetTimeSeriesAsync(
        TimeSeriesQueryParameters query,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Query by time range and label equivalent matchers
        var startUnixNano = query.StartTimestampUnixSec * 1_000_000_000;
        var endUnixNano = query.EndTimestampUnixSec * 1_000_000_000;

        var eqMatchers = new List<LabelMatcher>();
        var neqMatchers = new List<LabelMatcher>();
        var otherMatchers = new List<LabelMatcher>();

        foreach (var matcher in query.LabelMatchers)
        {
            switch (matcher.Type)
            {
                case LabelMatcherType.Equal:
                    eqMatchers.Add(matcher);
                    break;
                case LabelMatcherType.NotEqual:
                    neqMatchers.Add(matcher);
                    break;
                default:
                    otherMatchers.Add(matcher);
                    break;
            }
        }

        var queryable = collectionAccessor.Collection.Query()
            .Where(m => m.TimestampUnixNano >= startUnixNano && m.TimestampUnixNano <= endUnixNano);

        foreach (var eqMatcher in eqMatchers)
        {
            if (eqMatcher is { Name: Labels.MetricName, Type: LabelMatcherType.Equal })
            {
                var metricName = eqMatcher.Value;
                queryable = queryable.Where(m => m.Name == metricName);
                continue;
            }

            var labelString = $"{eqMatcher.Name}={eqMatcher.Value}";
            if (string.IsNullOrEmpty(labelString))
            {
                // If the value is empty in an Equal matcher (e.g., label = ""),
                // the label may not exist.
                queryable = queryable.Where(m =>
                    !m.LabelNames.Contains(eqMatcher.Name) || m.Labels.Contains(labelString));
            }
            else
            {
                queryable = queryable.Where(m => m.Labels.Contains(labelString));
            }
        }

        foreach (var neqMatcher in neqMatchers)
        {
            if (string.IsNullOrEmpty(neqMatcher.Value))
            {
                // If the value is empty in a NotEqual matcher (e.g., label != ""),
                // we need to ensure the label exists.
                queryable = queryable.Where(m => m.LabelNames.Contains(neqMatcher.Name));
            }

            var labelString = $"{neqMatcher.Name}={neqMatcher.Value}";
            queryable = queryable.Where(m => !m.Labels.Contains(labelString));
        }

        IEnumerable<LiteDBMetric> metrics = queryable
            .OrderBy(m => m.TimestampUnixNano)
            .Limit(query.Limit)
            .ToList();

        if (metrics.Any() && otherMatchers.Count != 0)
        {
            // Note: Other matcher types are not supported in this LiteDB implementation. We can only filter by ourselves.
            metrics = metrics.Where(metric =>
            {
                foreach (var matcher in otherMatchers)
                {
                    var labelPair = metric.Labels
                        .Select(lp => lp.Split('=', 2))
                        .FirstOrDefault(parts => parts.Length == 2 && parts[0] == matcher.Name);

                    var labelValue = labelPair?[1];

                    switch (matcher.Type)
                    {
                        case LabelMatcherType.NotEqual:
                            if (labelValue == matcher.Value)
                                return false;
                            break;
                        case LabelMatcherType.RegexMatch:
                            if (labelValue == null || !Regex.IsMatch(labelValue, matcher.Value))
                                return false;
                            break;
                        case LabelMatcherType.RegexNotMatch:
                            if (labelValue != null && Regex.IsMatch(labelValue, matcher.Value))
                                return false;
                            break;
                    }
                }

                return true;
            });
        }

        var series = TransformToTimeSeries(metrics);

        return Task.FromResult(series);
    }

    public Task<IEnumerable<string>> GetLabelNamesAsync(LabelNamesQueryParameters query)
    {
        var startUnixNano = query.StartTimestampUnixSec * 1_000_000_000;
        var endUnixNano = query.EndTimestampUnixSec * 1_000_000_000;
        var eqMatchers = query.LabelMatchers
            .Where(lm => lm.Type == LabelMatcherType.Equal)
            .ToList();

        var queryable = collectionAccessor.Collection.Query()
            .Where(m => m.TimestampUnixNano >= startUnixNano && m.TimestampUnixNano <= endUnixNano);

        foreach (var eqMatcher in eqMatchers)
        {
            var labelString = $"{eqMatcher.Name}={eqMatcher.Value}";
            queryable = queryable.Where(m => m.Labels.Contains(labelString));
        }

        var results = queryable.ToList();

        var labelNames = new HashSet<string>();
        foreach (var metric in results)
        {
            foreach (var labelPair in metric.Labels)
            {
                var parts = labelPair.Split('=', 2);
                if (parts.Length == 2)
                {
                    labelNames.Add(parts[0]);
                }
            }
        }

        return Task.FromResult<IEnumerable<string>>(labelNames);
    }

    public Task<IEnumerable<string>> GetLabelValuesAsync(LabelValuesQueryParameters query)
    {
        var startUnixNano = query.StartTimestampUnixSec * 1_000_000_000;
        var endUnixNano = query.EndTimestampUnixSec * 1_000_000_000;
        var queryable = collectionAccessor.Collection.Query()
            .Where(m => m.TimestampUnixNano >= startUnixNano && m.TimestampUnixNano <= endUnixNano)
            .Where(m => m.LabelNames.Contains(query.LabelName));

        var results = queryable.ToList();

        var labelValues = new HashSet<string>();
        foreach (var metric in results)
        {
            foreach (var labelPair in metric.Labels)
            {
                var parts = labelPair.Split('=', 2);
                if (parts.Length == 2 && parts[0] == query.LabelName)
                {
                    labelValues.Add(parts[1]);
                }
            }
        }

        return Task.FromResult<IEnumerable<string>>(labelValues);
    }

    private IEnumerable<TimeSeries> TransformToTimeSeries(IEnumerable<LiteDBMetric> metrics)
    {
        var grouped = new Dictionary<Labels, List<TimeSeriesSample>>();

        foreach (var metric in metrics)
        {
            var labels = new Labels();
            foreach (var labelPair in metric.Labels)
            {
                var parts = labelPair.Split('=', 2);
                if (parts.Length == 2)
                {
                    labels[parts[0]] = parts[1];
                }
            }

            if (!grouped.TryGetValue(labels, out var samples))
            {
                samples = new List<TimeSeriesSample>();
                grouped[labels] = samples;
            }

            samples.Add(new TimeSeriesSample
            {
                Value = metric.Value,
                TimestampUnixSec = metric.TimestampUnixNano / 1_000_000_000
            });
        }

        var timeSeriess = grouped
            .Select(kvp => new TimeSeries { Labels = kvp.Key, Samples = kvp.Value }).ToList();
        return timeSeriess;
    }
}
