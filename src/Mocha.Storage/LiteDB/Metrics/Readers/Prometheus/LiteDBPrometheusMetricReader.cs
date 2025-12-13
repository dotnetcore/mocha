// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using LiteDB;
using Microsoft.Extensions.Options;
using Mocha.Core.Models.Metrics;
using Mocha.Core.Storage.Prometheus;
using Mocha.Core.Storage.Prometheus.Metrics;
using Mocha.Storage.LiteDB.Metrics.Models;

namespace Mocha.Storage.LiteDB.Metrics.Readers.Prometheus;

public class LiteDBPrometheusMetricReader : IPrometheusMetricReader, IDisposable
{
    private readonly ILiteDatabase _db;
    private readonly ILiteCollection<LiteDBMetric> _collection;

    public LiteDBPrometheusMetricReader(IOptions<LiteDBMetricsOptions> optionsAccessor)
    {
        var options = optionsAccessor.Value;
        var dbPath = Path.Combine(options.DatabasePath, LiteDBConstants.MetricsDatabaseFileName);
        _db = LiteDBUtils.OpenDatabase(dbPath);
        _collection = _db.GetCollection<LiteDBMetric>(LiteDBConstants.MetricsCollectionName);

        BsonMapper.Global.Entity<LiteDBMetric>().Id(x => x.Id);
        _collection.EnsureIndex(x => x.TimestampUnixNano);
        _collection.EnsureIndex(x => x.Name);
        _collection.EnsureIndex(x => x.Labels);
        _collection.EnsureIndex(x => x.LabelNames);
    }

    public Task<IEnumerable<TimeSeries>> GetTimeSeriesAsync(
        TimeSeriesQueryParameters query,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Query by time range and label equivalent matchers
        var startUnixNano = query.StartTimestampUnixSec * 1_000_000_000;
        var endUnixNano = query.EndTimestampUnixSec * 1_000_000_000;
        var labelEqMatchers = query.LabelMatchers
            .Where(lm => lm.Type == LabelMatcherType.Equal)
            .ToList();
        var otherMatchers = query.LabelMatchers
            .Where(lm => lm.Type != LabelMatcherType.Equal)
            .ToList();

        var queryable = _collection.Query()
            .Where(m => m.TimestampUnixNano >= startUnixNano && m.TimestampUnixNano <= endUnixNano);

        foreach (var eqMatcher in labelEqMatchers)
        {
            if (eqMatcher is { Name: Labels.MetricName, Type: LabelMatcherType.Equal })
            {
                var metricName = eqMatcher.Value;
                queryable = queryable.Where(m => m.Name == metricName);
                continue;
            }

            var labelString = $"{eqMatcher.Name}={eqMatcher.Value}";
            queryable = queryable.Where(m => m.Labels.Contains(labelString));
        }

        var results = queryable
            .OrderBy(m => m.TimestampUnixNano)
            .ToList();

        if (!results.Any())
        {
            return Task.FromResult(Enumerable.Empty<TimeSeries>());
        }

        // Note: Other matcher types are not supported in this LiteDB implementation. We can only filter by ourselves.
        if (!otherMatchers.Any())
        {
            return Task.FromResult(TransformToTimeSeries(results));
        }

        var filteredResults = results.Where(metric =>
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

        return Task.FromResult(TransformToTimeSeries(filteredResults));
    }

    public Task<IEnumerable<string>> GetLabelNamesAsync(LabelNamesQueryParameters query)
    {
        var startUnixNano = query.StartTimestampUnixSec * 1_000_000_000;
        var endUnixNano = query.EndTimestampUnixSec * 1_000_000_000;
        var labelEqMatchers = query.LabelMatchers
            .Where(lm => lm.Type == LabelMatcherType.Equal)
            .ToList();
        var queryable = _collection.Query()
            .Where(m => m.TimestampUnixNano >= startUnixNano && m.TimestampUnixNano <= endUnixNano);

        foreach (var eqMatcher in labelEqMatchers)
        {
            queryable = queryable.Where(m => m.Labels.Contains($"{eqMatcher.Name}={eqMatcher.Value}"));
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
        var queryable = _collection.Query()
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

    public void Dispose()
    {
        _db.Dispose();
    }

    private IEnumerable<TimeSeries> TransformToTimeSeries(
        IEnumerable<LiteDBMetric> metrics)
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

        var timeSeriess = grouped.Select(kvp => new TimeSeries { Labels = kvp.Key, Samples = kvp.Value }).ToList();
        return timeSeriess;
    }
}
