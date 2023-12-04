// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Text;
using Mocha.Core.Enums;
using Mocha.Storage.EntityFrameworkCore.Trace;

namespace Mocha.Storage.EntityFrameworkCore;

public class OTelConverter
{
    public Span OTelSpanToEntityFrameworkSpan(OpenTelemetry.Proto.Trace.V1.Span span)
    {
        var traceId = Encoding.UTF8.GetString(span.TraceId.ToByteArray());
        var spanId = Encoding.UTF8.GetString(span.SpanId.ToByteArray());
        var entityFrameworkSpan = new Span()
        {
            SpanId = spanId,
            TraceFlags = span.Flags,
            TraceId = traceId,
            SpanName = span.Name,
            ParentSpanId = span.ParentSpanId.ToString() ?? "",
            StartTime = (long)span.StartTimeUnixNano,
            EndTime = (long)span.EndTimeUnixNano,
            Duration = (double)span.EndTimeUnixNano - span.StartTimeUnixNano,
            StatusCode = (int)span.Status.Code,
            StatusMessage = span.Status.Message,
            TraceState = span.TraceState,
            SpanKind = GetSpanKind(span.Kind),
        };
        var spanLinks = span.Links.Select(link => ConverterToSpanLink(link, traceId));
        var spanEvents = span.Events.Select(@event => ConverterToSpanEvent(@event, traceId));
        var spanAttributes = span.Attributes.Select(attribute => ConverterToSpanAttribute(attribute, traceId, spanId));
        entityFrameworkSpan.SpanAttributes = spanAttributes.ToList();
        entityFrameworkSpan.SpanEvents = spanEvents.ToList();
        entityFrameworkSpan.SpanLinks = spanLinks.ToList();
        return entityFrameworkSpan;
    }

    private static SpanAttribute ConverterToSpanAttribute(OpenTelemetry.Proto.Common.V1.KeyValue keyValue,
        string traceId, string spanId)
    {
        return new SpanAttribute()
        {
            AttributeKey = keyValue.Key,
            AttributeValue = keyValue.Value.StringValue,
            SpanId = spanId,
            TraceId = traceId,
        };
    }


    private static SpanEvent ConverterToSpanEvent(OpenTelemetry.Proto.Trace.V1.Span.Types.Event @event, string traceId)
    {
        return new SpanEvent()
        {
            TraceId = traceId,
            EventName = @event.Name,
            TimeBucket = (long)@event.TimeUnixNano
        };
    }


    private static SpanLink ConverterToSpanLink(OpenTelemetry.Proto.Trace.V1.Span.Types.Link link, string traceId)
    {
        return new SpanLink()
        {
            TraceId = link.TraceId.ToString() ?? "",
            SpanId = link.SpanId.ToString() ?? "",
            LinkedSpanId = traceId,
            TraceState = link.TraceState,
            Flags = link.Flags
        };
    }

    private static SpanKind GetSpanKind(OpenTelemetry.Proto.Trace.V1.Span.Types.SpanKind spanKind)
    {
        return spanKind switch
        {
            OpenTelemetry.Proto.Trace.V1.Span.Types.SpanKind.Unspecified => SpanKind.Unspecified,
            OpenTelemetry.Proto.Trace.V1.Span.Types.SpanKind.Internal => SpanKind.Internal,
            OpenTelemetry.Proto.Trace.V1.Span.Types.SpanKind.Server => SpanKind.Server,
            OpenTelemetry.Proto.Trace.V1.Span.Types.SpanKind.Client => SpanKind.Client,
            OpenTelemetry.Proto.Trace.V1.Span.Types.SpanKind.Producer => SpanKind.Producer,
            OpenTelemetry.Proto.Trace.V1.Span.Types.SpanKind.Consumer => SpanKind.Consumer,
            _ => throw new ArgumentOutOfRangeException(nameof(spanKind), spanKind, null)
        };
    }
}
