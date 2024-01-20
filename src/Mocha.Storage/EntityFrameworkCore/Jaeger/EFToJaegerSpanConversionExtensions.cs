// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Extensions;
using Mocha.Core.Storage.Jaeger.Trace;
using Mocha.Storage.EntityFrameworkCore.Trace;

namespace Mocha.Storage.EntityFrameworkCore.Jaeger;

internal static class EFToJaegerSpanConversionExtensions
{
    public static IEnumerable<JaegerTrace> ToJaegerTraces(
        this IEnumerable<EFSpan> spans,
        IEnumerable<EFSpanAttribute> spanAttributes,
        IEnumerable<EFResourceAttribute> resourceAttributes,
        IEnumerable<EFSpanEvent> spanEvents,
        IEnumerable<EFSpanEventAttribute> spanEventAttributes)
    {
        var spanAttributesBySpanId = spanAttributes
            .GroupBy(a => a.SpanId)
            .ToDictionary(g => g.Key, g => g.ToArray());

        var resourceAttributesBySpanId = resourceAttributes
            .GroupBy(a => a.SpanId)
            .ToDictionary(g => g.Key, g => g.ToArray());

        var spanEventsBySpanId = spanEvents
            .GroupBy(e => e.SpanId)
            .ToDictionary(g => g.Key, g => g.ToArray());

        var spanEventAttributesBySpanId = spanEventAttributes
            .GroupBy(a => a.SpanId)
            .ToDictionary(g => g.Key, g => g.ToArray());

        var jaegerSpans = new List<JaegerSpan>();

        foreach (var g in spans.GroupBy(s => s.SpanId))
        {
            var spanId = g.Key;
            var efSpans = g;

            spanAttributesBySpanId.TryGetValue(spanId, out var efSpanAttributes);
            resourceAttributesBySpanId.TryGetValue(spanId, out var efResourceAttributes);
            spanEventsBySpanId.TryGetValue(spanId, out var efSpanEvents);
            spanEventAttributesBySpanId.TryGetValue(spanId, out var efSpanEventAttributes);

            efSpanAttributes ??= Array.Empty<EFSpanAttribute>();
            efSpanEvents ??= Array.Empty<EFSpanEvent>();
            efSpanEventAttributes ??= Array.Empty<EFSpanEventAttribute>();

            jaegerSpans.AddRange(efSpans.ToJaegerSpans(efSpanAttributes, efSpanEvents, efSpanEventAttributes));
        }

        var jaegerTraces = jaegerSpans
            .GroupBy(s => s.TraceID)
            .Select(g =>
            {
                var spansOfCurrentTrace = g.ToArray();
                var jaegerProcesses = new List<JaegerProcess>();

                foreach (var span in spansOfCurrentTrace)
                {
                    resourceAttributesBySpanId.TryGetValue(span.SpanID, out var attributes);
                    attributes ??= Array.Empty<EFResourceAttribute>();
                    var process = new JaegerProcess
                    {
                        ProcessID = span.ProcessID,
                        ServiceName = attributes
                            .FirstOrDefault(a => a.Key == "service.name")?.Value ?? string.Empty,
                        Tags = Array.ConvertAll(attributes, ToJaegerTag)
                    };

                    jaegerProcesses.Add(process);
                }

                return new JaegerTrace
                {
                    TraceID = g.Key,
                    Processes = jaegerProcesses
                        .DistinctBy(p => p.ProcessID)
                        .ToDictionary(p => p.ProcessID),
                    Spans = spansOfCurrentTrace
                };
            });

        return jaegerTraces;
    }

    private static IEnumerable<JaegerSpan> ToJaegerSpans(
        this IEnumerable<EFSpan> spans,
        IEnumerable<EFSpanAttribute> spanAttributes,
        IEnumerable<EFSpanEvent> spanEvents,
        IEnumerable<EFSpanEventAttribute> spanEventAttributes)
    {
        foreach (var span in spans)
        {
            var jaegerSpan = new JaegerSpan
            {
                TraceID = span.TraceId,
                SpanID = span.SpanId,
                OperationName = span.SpanName,
                Flags = span.TraceFlags, // TODO: is this correct?
                StartTime = span.StartTimeUnixNano / 1000,
                Duration = span.DurationNanoseconds / 1000,
                ProcessID = span.ServiceInstanceId,
                References = string.IsNullOrWhiteSpace(span.ParentSpanId) // TODO: should we use span links?
                    ? Array.Empty<JaegerSpanReference>()
                    :
                    [
                        new JaegerSpanReference
                        {
                            TraceID = span.TraceId,
                            SpanID = span.ParentSpanId,
                            RefType = JaegerSpanReferenceType.ChildOf,
                        }
                    ],
                Tags = spanAttributes.ToJaegerSpanTags(span).ToArray(),
                Logs = spanEvents.ToJaegerSpanLogs(spanEventAttributes).ToArray()
            };

            yield return jaegerSpan;
        }
    }

    private static JaegerTag ToJaegerTag(this AbstractEFAttribute attribute)
    {
        var jaegerTag = new JaegerTag
        {
            Key = attribute.Key,
            Type = attribute.ValueType.ToJaegerTagType(),
            Value = ConvertTagValue(attribute.ValueType, attribute.Value)
        };

        return jaegerTag;
    }

    private static IEnumerable<JaegerTag> ToJaegerSpanTags(
        this IEnumerable<EFSpanAttribute> spanAttributes,
        EFSpan span)
    {
        if (span.StatusCode == EFSpanStatusCode.Error)
        {
            yield return new JaegerTag { Key = "error", Type = JaegerTagType.Bool, Value = true };
        }

        yield return new JaegerTag
        {
            Key = "span.kind",
            Type = JaegerTagType.String,
            Value = span.SpanKind.ToJaegerSpanKind()
        };

        foreach (var attribute in spanAttributes)
        {
            yield return attribute.ToJaegerTag();
        }
    }

    private static IEnumerable<JaegerSpanLog> ToJaegerSpanLogs(
        this IEnumerable<EFSpanEvent> spanEvents,
        IEnumerable<EFSpanEventAttribute> spanEventAttributes)
    {
        var attributesBySpanEvent = spanEventAttributes
            .GroupBy(a => a.SpanEventIndex)
            .ToDictionary(g => g.Key, g => g.ToArray());

        foreach (var spanEvent in spanEvents)
        {
            var jaegerSpanLog = new JaegerSpanLog
            {
                Timestamp = spanEvent.TimestampUnixNano / 1000,
                Fields = attributesBySpanEvent.TryGetValue(spanEvent.Index, out var attributes)
                    ? attributes.Select(a => new JaegerTag
                    {
                        Key = a.Key,
                        Type = a.ValueType.ToJaegerTagType(),
                        Value = a.Value
                    }).ToArray()
                    : Array.Empty<JaegerTag>()
            };

            yield return jaegerSpanLog;
        }
    }

    private static string ToJaegerTagType(this EFAttributeValueType valueType) => valueType switch
    {
        EFAttributeValueType.StringValue => JaegerTagType.String,
        EFAttributeValueType.BoolValue => JaegerTagType.Bool,
        EFAttributeValueType.IntValue => JaegerTagType.Int64,
        EFAttributeValueType.DoubleValue => JaegerTagType.Float64,
        // TODO: ArrayValue, KvlistValue, BytesValue
        EFAttributeValueType.ArrayValue => JaegerTagType.String,
        EFAttributeValueType.KvlistValue => JaegerTagType.String,
        EFAttributeValueType.BytesValue => JaegerTagType.String,
        _ => throw new ArgumentOutOfRangeException()
    };

    private static string ToJaegerSpanKind(this EFSpanKind spanKind) => spanKind switch
    {
        EFSpanKind.Internal => JaegerSpanKind.Internal,
        EFSpanKind.Server => JaegerSpanKind.Server,
        EFSpanKind.Client => JaegerSpanKind.Client,
        EFSpanKind.Producer => JaegerSpanKind.Producer,
        EFSpanKind.Consumer => JaegerSpanKind.Consumer,
        _ => JaegerSpanKind.Unspecified
    };

    private static object ConvertTagValue(this EFAttributeValueType valueType, string value) => valueType switch
    {
        EFAttributeValueType.StringValue => value,
        EFAttributeValueType.BoolValue => bool.Parse(value),
        EFAttributeValueType.IntValue => long.Parse(value),
        EFAttributeValueType.DoubleValue => double.Parse(value),
        // TODO: ArrayValue, KvlistValue, BytesValue
        EFAttributeValueType.ArrayValue => value,
        EFAttributeValueType.KvlistValue => value,
        EFAttributeValueType.BytesValue => value,
        _ => throw new ArgumentOutOfRangeException()
    };
}
