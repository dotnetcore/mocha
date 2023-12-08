using System.Text;
using Mocha.Core.Enums;
using Mocha.Storage.EntityFrameworkCore.Trace;
using OTelSpan = OpenTelemetry.Proto.Trace.V1.Span;
using OTelLink = OpenTelemetry.Proto.Trace.V1.Span.Types.Link;
using OTelEvent = OpenTelemetry.Proto.Trace.V1.Span.Types.Event;
using OTelKeyValue = OpenTelemetry.Proto.Common.V1.KeyValue;
using OTelSpanKind = OpenTelemetry.Proto.Trace.V1.Span.Types.SpanKind;

namespace Mocha.Storage.EntityFrameworkCore;
public static class EFConversionExtensions
{
    public static Span ToEFSpan(this OTelSpan span)
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

    private static SpanAttribute ToEFSpanAttribute(this OTelKeyValue keyValue, string traceId, string spanId)
    {
        return new SpanAttribute
        {
            AttributeKey = keyValue.Key,
            AttributeValue = keyValue.Value.StringValue,
            SpanId = spanId,
            TraceId = traceId,
        };
    }

    private static SpanEvent ToEFSpanEvent(this OTelEvent @event, string traceId)
    {
        return new SpanEvent { TraceId = traceId, EventName = @event.Name, TimeBucket = (long)@event.TimeUnixNano };
    }

    private static SpanLink ToEFSpanLink(this OTelLink link, string traceId)
    {
        return new SpanLink
        {
            TraceId = link.TraceId.ToString() ?? string.Empty,
            SpanId = link.SpanId.ToString() ?? string.Empty,
            LinkedSpanId = traceId,
            TraceState = link.TraceState,
            Flags = link.Flags
        };
    }

    private static SpanKind ToMochaSpanKind(this OTelSpanKind spanKind)
    {
        return spanKind switch
        {
            OTelSpanKind.Unspecified => SpanKind.Unspecified,
            OTelSpanKind.Internal => SpanKind.Internal,
            OTelSpanKind.Server => SpanKind.Server,
            OTelSpanKind.Client => SpanKind.Client,
            OTelSpanKind.Producer => SpanKind.Producer,
            OTelSpanKind.Consumer => SpanKind.Consumer,
            _ => throw new ArgumentOutOfRangeException(nameof(spanKind), spanKind, null)
        };
    }
}
