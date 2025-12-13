// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Storage.Prometheus.Metrics;

namespace Mocha.Core.Storage.Prometheus;

public interface IPrometheusMetricReader
{
    Task<IEnumerable<TimeSeries>> GetTimeSeriesAsync(TimeSeriesQueryParameters query, CancellationToken cancellationToken);

    Task<IEnumerable<string>> GetLabelNamesAsync(LabelNamesQueryParameters query);

    Task<IEnumerable<string>> GetLabelValuesAsync(LabelValuesQueryParameters query);
}
