// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Text;
using Mocha.Core.Enums;
using Mocha.Storage.EntityFrameworkCore.Trace;
using OTelLink = OpenTelemetry.Proto.Trace.V1.Span.Types.Link;
using OTelEvent = OpenTelemetry.Proto.Trace.V1.Span.Types.Event;
using OTelKeyValue = OpenTelemetry.Proto.Common.V1.KeyValue;


namespace Mocha.Storage.EntityFrameworkCore;

public static class ConverterToEntityFrameworkStorageModelExtensions
{
    public static Span OTelSpanToEntityFrameworkSpan(this OpenTelemetry.Proto.Trace.V1.Span span)
    {
        var traceId = Encoding.UTF8.GetString(span.TraceId.ToByteArray());
        var spanId = Encoding.UTF8.GetString(span.SpanId.ToByteArray());
        var parentSpanId = Encoding.UTF8.GetString(span.ParentSpanId.ToByteArray());
        var entityFrameworkSpan = new Span()
        {
            SpanId = spanId,
            TraceFlags = span.Flags,
            TraceId = traceId,
            SpanName = span.Name,
            ParentSpanId = parentSpanId,
            StartTime = (long) span.StartTimeUnixNano,
            EndTime = (long) span.EndTimeUnixNano,
            Duration = (double) span.EndTimeUnixNano - span.StartTimeUnixNano,
            StatusCode = (int) span.Status.Code,
            StatusMessage = span.Status.Message,
            TraceState = span.TraceState,
            SpanKind = span.Kind.ToMochaSpanKind(),
        };
        var spanLinks = span.Links.Select(link => link.ConverterToSpanLink(traceId));
        var spanEvents = span.Events.Select(@event => @event.ConverterToSpanEvent(traceId));
        var spanAttributes = span.Attributes.Select(attribute => attribute.ConverterToSpanAttribute(traceId, spanId));
        entityFrameworkSpan.SpanAttributes = spanAttributes.ToList();
        entityFrameworkSpan.SpanEvents = spanEvents.ToList();
        entityFrameworkSpan.SpanLinks = spanLinks.ToList();
        return entityFrameworkSpan;
    }

    private static SpanAttribute ConverterToSpanAttribute(this OTelKeyValue keyValue, string traceId, string spanId)
    {
        return new SpanAttribute
        {
            AttributeKey = keyValue.Key,
            AttributeValue = keyValue.Value.StringValue,
            SpanId = spanId,
            TraceId = traceId,
        };
    }


    private static SpanEvent ConverterToSpanEvent(this OTelEvent @event, string traceId)
    {
        return new SpanEvent { TraceId = traceId, EventName = @event.Name, TimeBucket = (long) @event.TimeUnixNano };
    }


    private static SpanLink ConverterToSpanLink(this OTelLink link, string traceId)
    {
        return new SpanLink
        {
            TraceId = link.TraceId.ToString() ?? "",
            SpanId = link.SpanId.ToString() ?? "",
            LinkedSpanId = traceId,
            TraceState = link.TraceState,
            Flags = link.Flags
        };
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="spanKind"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private static SpanKind ToMochaSpanKind(this OpenTelemetry.Proto.Trace.V1.Span.Types.SpanKind spanKind)
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
