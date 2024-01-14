// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Buffers.Binary;
using Google.Protobuf;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Resource.V1;
using OpenTelemetry.Proto.Trace.V1;

namespace Mocha.Core.Models.Trace;

public static class OTelToMochaSpanConversionExtensions
{
    public static MochaSpan ToMochaSpan(this Span span, Resource resource)
    {
        var traceId = ConvertByteStringToTraceId(span.TraceId);
        var spanId = ConvertByteStringToSpanId(span.SpanId);
        var parentSpanId = ConvertByteStringToSpanId(span.ParentSpanId);
        // TODO: no span link found from request
        var spanLinks = span.Links.Select(link => link.ToMochaSpanLink()).ToList();
        var spanEvents = span.Events.Select(@event => @event.ToMochaSpanEvent()).ToList();
        var attributes =
            span.Attributes.Select(attribute => attribute.ToMochaAttribute()).ToList();
        var serviceName = resource.Attributes
            .FirstOrDefault(attribute => attribute.Key == "service.name")?
            .Value?.StringValue ?? string.Empty;
        var serviceInstanceId = resource.Attributes
            .FirstOrDefault(attribute => attribute.Key == "service.instance.id")?
            .Value?.StringValue ?? string.Empty;

        var mochaSpan = new MochaSpan
        {
            SpanId = spanId,
            TraceFlags = span.Flags,
            TraceId = traceId,
            SpanName = span.Name,
            ParentSpanId = parentSpanId,
            StartTimeUnixNano = span.StartTimeUnixNano,
            EndTimeUnixNano = span.EndTimeUnixNano,
            DurationNanoseconds = span.EndTimeUnixNano - span.StartTimeUnixNano,
            StatusCode = (MochaSpanStatusCode?)span.Status?.Code,
            StatusMessage = span.Status?.Message,
            SpanKind = (MochaSpanKind)span.Kind,
            TraceState = span.TraceState,
            Attributes = attributes,
            Events = spanEvents,
            Links = spanLinks,
            Resource = new MochaResource
            {
                ServiceName = serviceName,
                ServiceInstanceId = serviceInstanceId,
                Attributes = resource.Attributes.Select(attribute => attribute.ToMochaAttribute()).ToList()
            }
        };

        return mochaSpan;
    }

    private static MochaSpanEvent ToMochaSpanEvent(this Span.Types.Event @event)
    {
        return new MochaSpanEvent
        {
            Name = @event.Name,
            Attributes = @event.Attributes.Select(attribute => attribute.ToMochaAttribute()).ToList(),
            TimestampUnixNano = @event.TimeUnixNano
        };
    }

    private static MochaSpanLink ToMochaSpanLink(this Span.Types.Link link)
    {
        return new MochaSpanLink
        {
            LinkedTraceId = ConvertByteStringToTraceId(link.TraceId),
            LinkedSpanId = ConvertByteStringToSpanId(link.SpanId),
            Attributes =
                link.Attributes.Select(attribute => attribute.ToMochaAttribute()).ToList(),
            LinkedTraceState = link.TraceState,
            LinkedTraceFlags = link.Flags
        };
    }

    private static MochaAttribute ToMochaAttribute(this KeyValue attribute)
    {
        return new MochaAttribute
        {
            Key = attribute.Key,
            ValueType = (MochaAttributeValueType)attribute.Value.ValueCase,
            Value = attribute.Value.ValueCase switch
            {
                AnyValue.ValueOneofCase.StringValue => attribute.Value.StringValue,
                AnyValue.ValueOneofCase.BoolValue => attribute.Value.BoolValue.ToString(),
                AnyValue.ValueOneofCase.IntValue => attribute.Value.IntValue.ToString(),
                AnyValue.ValueOneofCase.DoubleValue => attribute.Value.DoubleValue.ToString("R"),
                // TODO: Handle ArrayValue, KvlistValue, and BytesValue
                AnyValue.ValueOneofCase.ArrayValue => attribute.Value.ArrayValue.Values.ToString(),
                AnyValue.ValueOneofCase.KvlistValue => attribute.Value.KvlistValue.ToString(),
                AnyValue.ValueOneofCase.BytesValue => attribute.Value.BytesValue?.ToString() ?? string.Empty,
                _ => throw new ArgumentOutOfRangeException(nameof(attribute.Value.ValueCase),
                    attribute.Value.ValueCase,
                    "Unknown attribute value case.")
            }
        };
    }

    private static string ConvertByteStringToSpanId(ByteString byteString)
    {
        return byteString.Length == 0 ? string.Empty : ConvertBytesToLong(byteString.Span).ToString("x016");
    }

    private static string ConvertByteStringToTraceId(ByteString byteString)
    {
        if (byteString.Length == 0)
        {
            return string.Empty;
        }

        var high = ConvertBytesToLong(byteString.Span[..8]);
        var low = ConvertBytesToLong(byteString.Span[8..16]);
        return high == 0 ? low.ToString("x016") : $"{high:x016}{low:x016}";
    }

    private static long ConvertBytesToLong(ReadOnlySpan<byte> bytes) =>
        BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReadInt64BigEndian(bytes)
            : BinaryPrimitives.ReadInt64LittleEndian(bytes);
}
