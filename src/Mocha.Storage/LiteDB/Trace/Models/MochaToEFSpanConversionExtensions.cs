// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Models.Trace;

namespace Mocha.Storage.LiteDB.Trace.Models;

internal static class MochaToLiteDBSpanConversionExtensions
{
    internal static LiteDBSpan ToLiteDBSpan(this MochaSpan span)
    {
        var attributeKeyValueStrings = span.Attributes
            .Union(span.Resource.Attributes)
            .Select(a => $"{a.Key}={a.Value}").ToList();

        var liteDBSpan = new LiteDBSpan
        {
            TraceId = span.TraceId,
            SpanId = span.SpanId,
            SpanName = span.SpanName,
            ParentSpanId = span.ParentSpanId,
            StartTimeUnixNano = span.StartTimeUnixNano,
            EndTimeUnixNano = span.EndTimeUnixNano,
            DurationNanoseconds = span.DurationNanoseconds,
            StatusCode = (LiteDBSpanStatusCode?)span.StatusCode,
            StatusMessage = span.StatusMessage,
            SpanKind = (LiteDBSpanKind)span.SpanKind,
            ServiceName = span.Resource.ServiceName,
            ServiceInstanceId = span.Resource.ServiceInstanceId,
            TraceFlags = span.TraceFlags,
            TraceState = span.TraceState,
            Resource = span.Resource.ToLiteDBResource(),
            Links = span.Links.ToLiteDBSpanLink(),
            Attributes = span.Attributes.ToLiteDBAttributes(),
            AttributeKeyValueStrings = attributeKeyValueStrings,
            Events = span.Events.ToLiteDBSpanEvent()
        };

        return liteDBSpan;
    }


    private static LiteDBResource ToLiteDBResource(this MochaResource resource)
    {
        var liteDBResource = new LiteDBResource
        {
            ServiceName = resource.ServiceName,
            ServiceInstanceId = resource.ServiceInstanceId,
            Attributes = resource.Attributes.ToLiteDBAttributes()
        };

        return liteDBResource;
    }

    private static IEnumerable<LiteDBSpanLink> ToLiteDBSpanLink(this IEnumerable<MochaSpanLink> links)
    {
        var liteDBLinks = links.Select(link => new LiteDBSpanLink
        {
            LinkedTraceId = link.LinkedTraceId,
            LinkedSpanId = link.LinkedSpanId,
            Attributes = link.Attributes.ToLiteDBAttributes(),
            LinkedTraceState = link.LinkedTraceState,
            LinkedTraceFlags = link.LinkedTraceFlags
        });

        return liteDBLinks;
    }


    private static IEnumerable<LiteDBSpanEvent> ToLiteDBSpanEvent(this IEnumerable<MochaSpanEvent> spanEvents)
    {
        var liteDBSpanEvents = spanEvents.Select(spanEvent => new LiteDBSpanEvent
        {
            Name = spanEvent.Name,
            Attributes = spanEvent.Attributes.ToLiteDBAttributes(),
            TimestampUnixNano = spanEvent.TimestampUnixNano
        });

        return liteDBSpanEvents;
    }

    private static IEnumerable<LiteDBAttribute> ToLiteDBAttributes(this IEnumerable<MochaAttribute> attributes)
    {
        return attributes.Select(a => new LiteDBAttribute
        {
            Key = a.Key,
            ValueType = (LiteDBAttributeValueType)a.ValueType,
            Value = a.Value
        });
    }
}
