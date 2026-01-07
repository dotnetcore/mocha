// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Storage.Jaeger;
using Mocha.Core.Storage.Jaeger.Trace;
using Mocha.Storage.LiteDB.Trace.Models;

namespace Mocha.Storage.LiteDB.Trace.Readers.Jaeger;

internal class LiteDBJaegerSpanReader(ILiteDBCollectionAccessor<LiteDBSpan> collectionAccessor) : IJaegerSpanReader
{
    public Task<IEnumerable<JaegerTrace>> FindTracesAsync(JaegerTraceQueryParameters query)
    {
        var queryable = collectionAccessor.Collection.Query();

        // Filter by start time first so the index of StartTimeUnixNano can be used
        if (query.StartTimeMinUnixNano.HasValue)
        {
            queryable = queryable.Where(span => span.StartTimeUnixNano >= query.StartTimeMinUnixNano.Value);
        }

        if (query.StartTimeMaxUnixNano.HasValue)
        {
            queryable = queryable.Where(span => span.StartTimeUnixNano <= query.StartTimeMaxUnixNano.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.ServiceName))
        {
            queryable = queryable.Where(span => span.ServiceName == query.ServiceName);
        }

        if (!string.IsNullOrWhiteSpace(query.OperationName))
        {
            queryable = queryable.Where(span => span.SpanName == query.OperationName);
        }

        if (query.Tags is not null)
        {
            foreach (var (key, value) in query.Tags)
            {
                var tagString = $"{key}={value}";
                queryable = queryable.Where(span => span.AttributeKeyValueStrings.Contains(tagString));
            }
        }

        if (query.DurationMinNanoseconds.HasValue)
        {
            queryable = queryable.Where(span => span.DurationNanoseconds >= query.DurationMinNanoseconds.Value);
        }

        if (query.DurationMaxNanoseconds.HasValue)
        {
            queryable = queryable.Where(span => span.DurationNanoseconds <= query.DurationMaxNanoseconds.Value);
        }

        queryable = queryable.OrderByDescending(span => span.StartTimeUnixNano);

        var spans = query.NumTraces > 0 ? queryable.Limit(query.NumTraces).ToList() : queryable.ToList();

        var jaegerTraces = spans.ToJaegerTraces();

        return Task.FromResult(jaegerTraces);
    }

    public Task<IEnumerable<JaegerTrace>> FindTracesAsync(
        string[] traceIDs,
        ulong? startTimeMinUnixNano = null,
        ulong? startTimeMaxUnixNano = null)
    {
        var queryable = collectionAccessor.Collection.Query()
            .Where(span => traceIDs.Contains(span.TraceId));

        if (startTimeMinUnixNano.HasValue)
        {
            queryable = queryable.Where(span => span.StartTimeUnixNano >= startTimeMinUnixNano.Value);
        }

        if (startTimeMaxUnixNano.HasValue)
        {
            queryable = queryable.Where(span => span.StartTimeUnixNano <= startTimeMaxUnixNano.Value);
        }

        var spans = queryable.ToList();

        var jaegerTraces = spans.ToJaegerTraces();

        return Task.FromResult(jaegerTraces);
    }
}
