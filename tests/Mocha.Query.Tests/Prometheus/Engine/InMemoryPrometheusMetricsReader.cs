// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Mocha.Core.Storage.Prometheus;
using Mocha.Core.Storage.Prometheus.Metrics;

namespace Mocha.Query.Tests.Prometheus.Engine;

public class InMemoryPrometheusMetricsReader(IEnumerable<TimeSeries> timeSeries) : IPrometheusMetricsReader
{
    public Task<IEnumerable<TimeSeries>> GetTimeSeriesAsync(
        TimeSeriesQueryParameters query,
        CancellationToken cancellationToken)
    {
        IEnumerable<TimeSeries> result = timeSeries
            .Where(s =>
            {
                foreach (var lm in query.LabelMatchers)
                {
                    switch (lm.Type)
                    {
                        case LabelMatcherType.Equal:
                            if (!s.Labels.TryGetValue(lm.Name, out var labelValue) || labelValue != lm.Value)
                            {
                                return false;
                            }

                            break;
                        case LabelMatcherType.NotEqual:
                            if (s.Labels.TryGetValue(lm.Name, out labelValue) && labelValue == lm.Value)
                            {
                                return false;
                            }

                            break;
                        case LabelMatcherType.RegexMatch:
                            if (!s.Labels.TryGetValue(lm.Name, out labelValue) ||
                                !Regex.IsMatch(labelValue, lm.Value))
                            {
                                return false;
                            }

                            break;
                        case LabelMatcherType.RegexNotMatch:
                            if (s.Labels.TryGetValue(lm.Name, out var value) && Regex.IsMatch(value, lm.Value))
                            {
                                return false;
                            }

                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                return true;
            }).ToList();
        return Task.FromResult(result);
    }

    public Task<IEnumerable<string>> GetLabelNamesAsync(LabelNamesQueryParameters query)
    {
        return Task.FromResult(Enumerable.Empty<string>());
    }

    public Task<IEnumerable<string>> GetLabelValuesAsync(LabelValuesQueryParameters query)
    {
        return Task.FromResult(Enumerable.Empty<string>());
    }
}
