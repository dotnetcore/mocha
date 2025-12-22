// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using LiteDB;
using Microsoft.Extensions.Options;
using Mocha.Storage.LiteDB.Trace.Models;

namespace Mocha.Storage.LiteDB.Trace;

public class LiteDBSpansCollectionAccessor : LiteDBCollectionAccessor<LiteDBSpan>
{
    public LiteDBSpansCollectionAccessor(IOptions<LiteDBTracingOptions> options)
        : base(
            Path.Combine(options.Value.DatabasePath, LiteDBConstants.SpansDatabaseFileName),
            LiteDBConstants.SpansCollectionName)
    {
    }

    protected override void ConfigureCollection(ILiteCollection<LiteDBSpan> collection)
    {
        BsonMapper.Global.Entity<LiteDBSpan>().Id(x => x.Id);
        collection.EnsureIndex(x => x.TraceId);
        collection.EnsureIndex(x => x.SpanId);
        collection.EnsureIndex(x => x.ServiceName);
        collection.EnsureIndex(x => x.SpanName);
        collection.EnsureIndex(x => x.StartTimeUnixNano);
        collection.EnsureIndex(x => x.EndTimeUnixNano);
        collection.EnsureIndex(x => x.DurationNanoseconds);
    }
}
