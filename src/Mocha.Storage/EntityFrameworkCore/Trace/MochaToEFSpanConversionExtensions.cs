// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Models.Trace;

namespace Mocha.Storage.EntityFrameworkCore.Trace;

internal static class MochaToEFSpanConversionExtensions
{
    internal static EFSpan ToEFSpan(this MochaSpan span)
    {
        var efSpan = new EFSpan
        {
            TraceId = span.TraceId,
            SpanId = span.SpanId,
            SpanName = span.SpanName,
            ParentSpanId = span.ParentSpanId,
            StartTimeUnixNano = span.StartTimeUnixNano,
            EndTimeUnixNano = span.EndTimeUnixNano,
            DurationNanoseconds = span.DurationNanoseconds,
            StatusCode = (EFSpanStatusCode?)span.StatusCode,
            StatusMessage = span.StatusMessage,
            SpanKind = (EFSpanKind)span.SpanKind,
            ServiceName = span.Resource.ServiceName,
            ServiceInstanceId = span.Resource.ServiceInstanceId,
            TraceFlags = span.TraceFlags,
            TraceState = span.TraceState
        };

        return efSpan;
    }

    public static IEnumerable<EFSpanAttribute> ToEFSpanAttributes(this MochaSpan span)
    {
        return span.Attributes.Select(a => new EFSpanAttribute
        {
            TraceId = span.TraceId,
            SpanId = span.SpanId,
            Key = a.Key,
            ValueType = (EFAttributeValueType)a.ValueType,
            Value = a.Value
        });
    }

    public static IEnumerable<EFResourceAttribute> ToEFResourceAttributes(this MochaSpan span)
    {
        return span.Resource.Attributes.Select(a => new EFResourceAttribute
        {
            TraceId = span.TraceId,
            SpanId = span.SpanId,
            Key = a.Key,
            ValueType = (EFAttributeValueType)a.ValueType,
            Value = a.Value
        });
    }

    public static EFSpanEvent ToEFSpanEvent(this MochaSpanEvent spanEvent, MochaSpan span, int spanEventIndex)
    {
        var efSpanEvent = new EFSpanEvent
        {
            TraceId = span.TraceId,
            SpanId = span.SpanId,
            Index = spanEventIndex,
            Name = spanEvent.Name,
            TimestampUnixNano = spanEvent.TimestampUnixNano
        };

        return efSpanEvent;
    }

    public static IEnumerable<EFSpanEventAttribute> ToEFSpanEventAttributes(
        this MochaSpanEvent spanEvent,
        EFSpanEvent efSpanEvent)
    {
        return spanEvent.Attributes.Select(a => new EFSpanEventAttribute
        {
            TraceId = efSpanEvent.TraceId,
            SpanId = efSpanEvent.SpanId,
            SpanEventIndex = efSpanEvent.Index,
            Key = a.Key,
            ValueType = (EFAttributeValueType)a.ValueType,
            Value = a.Value
        });
    }

    public static EFSpanLink ToEFSpanLink(this MochaSpanLink link, MochaSpan span, int spanLinkIndex)
    {
        var efLink = new EFSpanLink
        {
            TraceId = span.TraceId,
            SpanId = span.SpanId,
            Index = spanLinkIndex,
            LinkedTraceId = link.LinkedTraceId,
            LinkedSpanId = link.LinkedSpanId,
            LinkedTraceState = link.LinkedTraceState,
            LinkedTraceFlags = link.LinkedTraceFlags
        };

        return efLink;
    }

    public static IEnumerable<EFSpanLinkAttribute> ToEFSpanLinkAttributes(
        this MochaSpanLink spanLink,
        EFSpanLink efSpanLink)
    {
        return spanLink.Attributes.Select(a => new EFSpanLinkAttribute
        {
            TraceId = efSpanLink.TraceId,
            SpanId = efSpanLink.SpanId,
            SpanLinkIndex = efSpanLink.Index,
            Key = a.Key,
            ValueType = (EFAttributeValueType)a.ValueType,
            Value = a.Value
        });
    }
}
