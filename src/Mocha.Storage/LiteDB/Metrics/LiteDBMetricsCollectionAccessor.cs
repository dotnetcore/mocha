// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using LiteDB;
using Microsoft.Extensions.Options;
using Mocha.Storage.LiteDB.Metrics.Models;

namespace Mocha.Storage.LiteDB.Metrics;

internal class LiteDBMetricsCollectionAccessor(IOptions<LiteDBMetricsOptions> optionsAccessor)
    : LiteDBCollectionAccessor<LiteDBMetric>(
            optionsAccessor.Value.DatabasePath,
            LiteDBConstants.MetricsDatabaseFileName,
            LiteDBConstants.MetricsCollectionName)
{
    protected override void ConfigureCollection(ILiteCollection<LiteDBMetric> collection)
    {
        // Even if multiple indexed expressions are used on a query, only one of the indexes is used,
        // with the remaining expressions being filtered using a full scan.
        // Therefore, we create only one index on TimestampUnixNano to optimize time range queries.
        BsonMapper.Global.Entity<LiteDBMetric>().Id(x => x.Id);
        collection.EnsureIndex(x => x.TimestampUnixNano); ;
    }
}
