// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using LiteDB;
using Microsoft.Extensions.Options;
using Mocha.Storage.LiteDB.Trace.Models;

namespace Mocha.Storage.LiteDB.Trace;

public class LiteDBSpansCollectionAccessor : LiteDBCollectionAccessor<LiteDBSpan>
{
    public LiteDBSpansCollectionAccessor(IOptions<LiteDBTracingOptions> optionsAccessor)
        : base(
            optionsAccessor.Value.DatabasePath,
            LiteDBConstants.SpansDatabaseFileName,
            LiteDBConstants.SpansCollectionName)
    {
    }

    protected override void ConfigureCollection(ILiteCollection<LiteDBSpan> collection)
    {
        // Even if multiple indexed expressions are used on a query, only one of the indexes is used,
        // with the remaining expressions being filtered using a full scan.
        // Therefore, we create only two indexes: one on StartTimeUnixNano to optimize time range queries,
        // and another on TraceId to optimize trace ID lookups.
        BsonMapper.Global.Entity<LiteDBSpan>().Id(x => x.SpanId);
        collection.EnsureIndex(x => x.StartTimeUnixNano);
        collection.EnsureIndex(x => x.TraceId);
    }
}
