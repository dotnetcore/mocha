// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using LiteDB;
using Microsoft.Extensions.Options;
using Mocha.Storage.LiteDB.Metadata.Models;

namespace Mocha.Storage.LiteDB.Metadata;

internal class LiteDBSpanMetadataCollectionAccessor(IOptions<LiteDBMetadataOptions> optionsAccessor)
    : LiteDBCollectionAccessor<LiteDBSpanMetadata>(
        optionsAccessor.Value.DatabasePath,
        LiteDBConstants.SpansMetadataDatabaseFileName,
        LiteDBConstants.SpansMetadataCollectionName)
{
    protected override void ConfigureCollection(ILiteCollection<LiteDBSpanMetadata> collection)
    {
        BsonMapper.Global.Entity<LiteDBSpanMetadata>().Id(x => x.Id);
        collection.EnsureIndex(x => x.ServiceName);
        collection.EnsureIndex(x => x.OperationName);
    }
}
