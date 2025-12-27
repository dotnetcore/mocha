// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using LiteDB;
using Microsoft.Extensions.Options;
using Mocha.Storage.LiteDB.Metadata.Models;

namespace Mocha.Storage.LiteDB.Metadata;

internal class LiteDBMetricMetadataCollectionAccessor(IOptions<LiteDBMetadataOptions> optionsAccessor)
    : LiteDBCollectionAccessor<LiteDBMetricMetadata>(
        optionsAccessor.Value.DatabasePath,
        LiteDBConstants.MetricsMetadataDatabaseFileName,
        LiteDBConstants.MetricsMetadataCollectionName)
{
    protected override void ConfigureCollection(ILiteCollection<LiteDBMetricMetadata> collection)
    {
        BsonMapper.Global.Entity<LiteDBMetricMetadata>().Id(x => x.Id);
        collection.EnsureIndex(x => x.Metric);
    }
}
