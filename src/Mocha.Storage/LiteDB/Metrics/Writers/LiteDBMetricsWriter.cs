// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Models.Metrics;
using Mocha.Core.Storage;
using Mocha.Storage.LiteDB.Metrics.Models;

namespace Mocha.Storage.LiteDB.Metrics.Writers;

internal class LiteDBMetricsWriter(
    ILiteDBCollectionAccessor<LiteDBMetric> collectionAccessor)
    : ITelemetryDataWriter<MochaMetric>
{
    public Task WriteAsync(IEnumerable<MochaMetric> data)
    {
        var liteDBMetrics = data.Select(metric => new LiteDBMetric
        {
            Name = metric.Name,
            Type = metric.Type,
            Unit = metric.Unit,
            Labels = metric.Labels.Select(l => $"{l.Key}={l.Value}").ToArray(),
            LabelNames = metric.Labels.Keys.ToArray(),
            Value = metric.Value,
            TimestampUnixNano = (long)metric.TimestampUnixNano
        });

        collectionAccessor.Collection.InsertBulk(liteDBMetrics);
        return Task.CompletedTask;
    }
}
