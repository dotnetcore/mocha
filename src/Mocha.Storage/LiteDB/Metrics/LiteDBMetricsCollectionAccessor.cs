// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using LiteDB;
using Microsoft.Extensions.Options;
using Mocha.Storage.LiteDB.Metrics.Models;

namespace Mocha.Storage.LiteDB.Metrics;

internal class LiteDBMetricsCollectionAccessor(IOptions<LiteDBMetricsOptions> optionsAccessor)
    : LiteDBCollectionAccessor<LiteDBMetric>(
            Path.Combine(optionsAccessor.Value.DatabasePath, LiteDBConstants.MetricsDatabaseFileName),
            LiteDBConstants.MetricsCollectionName)
{
    protected override void ConfigureCollection(ILiteCollection<LiteDBMetric> collection)
    {
        BsonMapper.Global.Entity<LiteDBMetric>().Id(x => x.Id);
        collection.EnsureIndex(x => x.TimestampUnixNano);
        collection.EnsureIndex(x => x.Name);
        collection.EnsureIndex(x => x.Labels);
        collection.EnsureIndex(x => x.LabelNames);
    }
}
