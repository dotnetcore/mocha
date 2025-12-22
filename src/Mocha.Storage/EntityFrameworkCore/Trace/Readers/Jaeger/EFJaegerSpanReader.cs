// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Mocha.Core.Storage.Jaeger;
using Mocha.Core.Storage.Jaeger.Trace;
using Mocha.Storage.EntityFrameworkCore.Trace.Models;

namespace Mocha.Storage.EntityFrameworkCore.Trace.Readers.Jaeger;

internal class EFJaegerSpanReader(IDbContextFactory<MochaTraceContext> contextFactory) : IJaegerSpanReader
{
    public async Task<IEnumerable<JaegerTrace>> FindTracesAsync(JaegerTraceQueryParameters query)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var queryableSpans = context.Spans.AsQueryable();

        if (!string.IsNullOrEmpty(query.ServiceName))
        {
            queryableSpans = queryableSpans.Where(s => s.ServiceName == query.ServiceName);
        }

        if (!string.IsNullOrEmpty(query.OperationName))
        {
            queryableSpans = queryableSpans.Where(s => s.SpanName == query.OperationName);
        }

        if (query.StartTimeMinUnixNano.HasValue)
        {
            queryableSpans = queryableSpans.Where(s => s.StartTimeUnixNano >= query.StartTimeMinUnixNano.Value);
        }

        if (query.StartTimeMaxUnixNano.HasValue)
        {
            queryableSpans = queryableSpans.Where(s => s.StartTimeUnixNano <= query.StartTimeMaxUnixNano.Value);
        }

        if (query.DurationMinNanoseconds.HasValue)
        {
            queryableSpans =
                queryableSpans.Where(s => s.DurationNanoseconds >= query.DurationMinNanoseconds.Value);
        }

        if (query.DurationMaxNanoseconds.HasValue)
        {
            queryableSpans =
                queryableSpans.Where(s => s.DurationNanoseconds <= query.DurationMaxNanoseconds.Value);
        }

        if (query.Tags?.Any() ?? false)
        {
            // TODO: This is a hacky way to do this, but it works for now. We should find a better way to match tags.
            var tags = query.Tags.Select(tag => $"{tag.Key}:{tag.Value}").ToHashSet();
            var queryableAttributes =
                context.SpanAttributes
                    .Where(a => tags.Contains(a.Key + ":" + a.Value));

            var spanIds = queryableAttributes.GroupBy(a => a.SpanId)
                .Where(a => a.Count() == query.Tags.Count())
                .Select(a => a.Key);

            queryableSpans = from span in queryableSpans
                             join spanId in spanIds on span.SpanId equals spanId
                             select span;
        }

        queryableSpans = queryableSpans.OrderByDescending(s => s.Id);

        if (query.NumTraces > 0)
        {
            queryableSpans = queryableSpans.Take(query.NumTraces);
        }

        return await QueryJaegerTracesAsync(queryableSpans, context);
    }

    public async Task<IEnumerable<JaegerTrace>> FindTracesAsync(
        string[]? traceIDs,
        ulong? startTimeUnixNano,
        ulong? endTimeUnixNano)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var queryableSpans = context.Spans.AsQueryable();

        if (traceIDs?.Any() ?? false)
        {
            queryableSpans = queryableSpans.Where(s => traceIDs.Contains(s.TraceId));
        }

        if (startTimeUnixNano.HasValue)
        {
            queryableSpans = queryableSpans.Where(s => s.StartTimeUnixNano >= startTimeUnixNano.Value);
        }

        if (endTimeUnixNano.HasValue)
        {
            queryableSpans = queryableSpans.Where(s => s.StartTimeUnixNano <= endTimeUnixNano.Value);
        }

        return await QueryJaegerTracesAsync(queryableSpans, context);
    }

    private static async Task<IEnumerable<JaegerTrace>> QueryJaegerTracesAsync(
        IQueryable<EFSpan> queryableSpans,
        MochaTraceContext context)
    {
        var spans = await queryableSpans.ToListAsync();

        var spanIds = spans.Select(s => s.SpanId).ToArray();

        var spanAttributes = await context.SpanAttributes
            .Where(a => spanIds.Contains(a.SpanId))
            .ToListAsync();

        var resourceAttributes = await context.ResourceAttributes
            .Where(a => spanIds.Contains(a.SpanId))
            .ToListAsync();

        var spanEvents = await context.SpanEvents
            .Where(e => spanIds.Contains(e.SpanId))
            .ToListAsync();

        var spanEventAttributes = await context.SpanEventAttributes
            .Where(a => spanIds.Contains(a.SpanId))
            .ToListAsync();

        var jaegerTraces = spans.ToJaegerTraces(
            spanAttributes, resourceAttributes, spanEvents, spanEventAttributes).ToArray();

        return jaegerTraces;
    }
}
