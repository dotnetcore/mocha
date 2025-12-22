// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Models.Trace;
using Mocha.Core.Storage;
using Mocha.Storage.LiteDB.Trace.Models;

namespace Mocha.Storage.LiteDB.Trace.Writers;

internal class LiteDBSpanWriter(ILiteDBCollectionAccessor<LiteDBSpan> collectionAccessor)
    : ITelemetryDataWriter<MochaSpan>
{
    public Task WriteAsync(IEnumerable<MochaSpan> data)
    {
        var liteDBSpans = data.Select(span => span.ToLiteDBSpan());

        collectionAccessor.Collection.InsertBulk(liteDBSpans);
        return Task.CompletedTask;
    }
}
