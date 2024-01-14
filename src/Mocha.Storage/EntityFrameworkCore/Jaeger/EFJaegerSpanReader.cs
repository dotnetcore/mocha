// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Mocha.Core.Storage.Jaeger;
using Mocha.Core.Storage.Jaeger.Trace;
using Mocha.Storage.EntityFrameworkCore.Trace;

namespace Mocha.Storage.EntityFrameworkCore.Jaeger;

internal class EFJaegerSpanReader(IDbContextFactory<MochaContext> contextFactory) : IJaegerSpanReader
{
    public async Task<string[]> GetSeriesAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var services = await context.Spans.Select(s => s.ServiceName).Distinct().ToArrayAsync();
        return services;
    }

    public async Task<string[]> GetOperationsAsync(string serviceName)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var operations = await context.Spans
            .Where(s => s.ServiceName == serviceName)
            .Select(s => s.SpanName)
            .Distinct()
            .ToArrayAsync();
        return operations;
    }

    public async Task<JaegerTrace[]> FindTracesAsync(JaegerTraceQueryParameters query)
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

        if (query.Tags?.Any() ?? false)
        {
            var queryableAttributes = context.SpanAttributes.AsQueryable();

            foreach (var tag in query.Tags)
            {
                queryableAttributes =
                    queryableAttributes.Where(a => a.Key == tag.Key && a.Value == tag.Value.ToString());
            }

            var ids = queryableAttributes.Select(a => a.SpanId).Distinct();

            queryableSpans = queryableSpans.Where(s => ids.Contains(s.SpanId));
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

        if (query.NumTraces > 0)
        {
            queryableSpans = queryableSpans
                .OrderByDescending(s => s.Id)
                .Take(query.NumTraces);
        }

        return await QueryJaegerTracesAsync(queryableSpans, context);
    }

    public async Task<JaegerTrace[]> FindTracesAsync(
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

    private static async Task<JaegerTrace[]> QueryJaegerTracesAsync(
        IQueryable<EFSpan> queryableSpans,
        MochaContext context)
    {
        var spans = await queryableSpans.ToArrayAsync();

        var spanIds = spans.Select(s => s.SpanId).ToArray();

        var spanAttributes = await context.SpanAttributes
            .Where(a => spanIds.Contains(a.SpanId))
            .ToArrayAsync();

        var resourceAttributes = await context.ResourceAttributes
            .Where(a => spanIds.Contains(a.SpanId))
            .ToArrayAsync();

        var spanEvents = await context.SpanEvents
            .Where(e => spanIds.Contains(e.SpanId))
            .ToArrayAsync();

        var spanEventAttributes = await context.SpanEventAttributes
            .Where(a => spanIds.Contains(a.SpanId))
            .ToArrayAsync();

        var jaegerTraces = spans.ToJaegerTraces(
            spanAttributes, resourceAttributes, spanEvents, spanEventAttributes).ToArray();

        return jaegerTraces;
    }
}
