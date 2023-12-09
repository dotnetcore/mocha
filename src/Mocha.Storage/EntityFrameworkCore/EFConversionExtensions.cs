// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Text;
using Mocha.Storage.EntityFrameworkCore.Trace;

namespace Mocha.Storage.EntityFrameworkCore;

public static class EFConversionExtensions
{

    public static EFSpan ToEFSpan(this OpenTelemetry.Proto.Trace.V1.Span span)
    {
        var traceId = Encoding.UTF8.GetString(span.TraceId.ToByteArray());
        var spanId = Encoding.UTF8.GetString(span.SpanId.ToByteArray());
        var parentSpanId = Encoding.UTF8.GetString(span.ParentSpanId.ToByteArray());
        var entityFrameworkSpan = new EFSpan()
        {
            SpanId = spanId,
            TraceFlags = span.Flags,
            TraceId = traceId,
            SpanName = span.Name,
            ParentSpanId = parentSpanId,
            StartTime = (long)span.StartTimeUnixNano,
            EndTime = (long)span.EndTimeUnixNano,
            Duration = (double)span.EndTimeUnixNano - span.StartTimeUnixNano,
            StatusCode = (int)span.Status.Code,
            StatusMessage = span.Status.Message,
            TraceState = span.TraceState,
            SpanKind = span.Kind.ToMochaSpanKind(),
        };
        var spanLinks = span.Links.Select(link => link.ToEFSpanLink(traceId));
        var spanEvents = span.Events.Select(@event => @event.ToEFSpanEvent(traceId));
        var spanAttributes = span.Attributes.Select(attribute => attribute.ToEFSpanAttribute(traceId, spanId));
        entityFrameworkSpan.SpanAttributes = spanAttributes.ToList();
        entityFrameworkSpan.SpanEvents = spanEvents.ToList();
        entityFrameworkSpan.SpanLinks = spanLinks.ToList();
        return entityFrameworkSpan;
    }

    private static EFSpanAttribute ToEFSpanAttribute(this OpenTelemetry.Proto.Common.V1.KeyValue keyValue, string traceId,
        string spanId)
    {
        return new EFSpanAttribute
        {
            AttributeKey = keyValue.Key,
            AttributeValue = keyValue.Value.StringValue,
            SpanId = spanId,
            TraceId = traceId,
        };
    }

    private static EFSpanEvent ToEFSpanEvent(this OpenTelemetry.Proto.Trace.V1.Span.Types.Event @event, string traceId)
    {
        return new EFSpanEvent
        {
            TraceId = traceId,
            EventName = @event.Name,
            TimeBucket = (long)@event.TimeUnixNano
        };
    }

    private static EFSpanLink ToEFSpanLink(this OpenTelemetry.Proto.Trace.V1.Span.Types.Link link, string traceId)
    {
        return new EFSpanLink
        {
            TraceId = link.TraceId.ToString() ?? string.Empty,
            SpanId = link.SpanId.ToString() ?? string.Empty,
            LinkedSpanId = traceId,
            TraceState = link.TraceState,
            Flags = link.Flags
        };
    }
}
