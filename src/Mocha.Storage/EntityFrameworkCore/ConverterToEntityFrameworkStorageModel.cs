// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Enums;
using Mocha.Storage.EntityFrameworkCore.Trace;

namespace Mocha.Storage.EntityFrameworkCore;

public class ConverterToEntityFrameworkStorageModel : IConverterToEntityFrameworkStorageModel
{
    public Span ConverterToSpan(OpenTelemetry.Proto.Trace.V1.Span span)
    {
        var spanId = span.SpanId.ToString() ?? "";
        var traceId = span.TraceId.ToString() ?? "";
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
        var spanLinks = span.Links.Select(ConverterToSpanLink);
        var spanEvents = span.Events.Select(ConverterToSpanEvent);
        var spanAttributes = span.Attributes.Select(attribute => ConverterToSpanAttribute(attribute, traceId, spanId));
        entityFrameworkSpan.SpanAttributes = spanAttributes;
        entityFrameworkSpan.SpanEvents = spanEvents;

        entityFrameworkSpan.SpanLinks = spanLinks;
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


    private static SpanEvent ConverterToSpanEvent(OpenTelemetry.Proto.Trace.V1.Span.Types.Event @event)
    {
        return new SpanEvent() { };
    }


    private static SpanLink ConverterToSpanLink(OpenTelemetry.Proto.Trace.V1.Span.Types.Link link)
    {
        return new SpanLink()
        {
            TraceId = link.TraceId.ToString() ?? "",
            SpanId = link.SpanId.ToString() ?? "",
            LinkedSpanId = link.TraceId.ToString() ?? "",
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
