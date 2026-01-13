// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Storage.Jaeger.Trace;
using Mocha.Storage.LiteDB.Trace.Models;

namespace Mocha.Storage.LiteDB.Trace.Readers.Jaeger;

internal static class LiteDBToJaegerSpanConversionExtensions
{
    public static IEnumerable<JaegerTrace> ToJaegerTraces(
        this IEnumerable<LiteDBSpan> spans)
    {
        var jaegerTraces = spans
            .GroupBy(x => x.TraceId)
            .Select(g =>
            {
                var jaegerProcesses = new Dictionary<string, JaegerProcess>();

                foreach (var span in g)
                {
                    var process = new JaegerProcess
                    {
                        ServiceName = span.ServiceName,
                        // service.instance.id may be null
                        ProcessID =
                            string.IsNullOrWhiteSpace(span.ServiceInstanceId)
                                ? span.ServiceName
                                : span.ServiceInstanceId,
                        Tags = span.Resource.Attributes.Select(a => a.ToJaegerTag()).ToArray()
                    };

                    jaegerProcesses.TryAdd(process.ProcessID, process);
                }

                return new JaegerTrace
                {
                    TraceID = g.Key,
                    Processes = jaegerProcesses,
                    Spans = g.Select(span => span.ToJaegerSpan()).ToArray(),
                };
            });

        return jaegerTraces;
    }

    public static JaegerSpan ToJaegerSpan(this LiteDBSpan span)
    {
        return new JaegerSpan
        {
            TraceID = span.TraceId,
            SpanID = span.SpanId,
            OperationName = span.SpanName,
            Flags = span.TraceFlags,
            StartTime = span.StartTimeUnixNano / 1000,
            Duration = span.DurationNanoseconds / 1000,
            ProcessID = span.ServiceInstanceId,
            References =
                string.IsNullOrWhiteSpace(span.ParentSpanId)
                    ? []
                    :
                    [
                        new JaegerSpanReference
                        {
                            RefType = JaegerSpanReferenceType.ChildOf,
                            TraceID = span.TraceId,
                            SpanID = span.ParentSpanId
                        }
                    ],
            Tags = span.Attributes.ToJaegerSpanTags(span).ToArray(),
            Logs = span.Events.ToJaegerSpanLogs().ToArray()
        };
    }

    private static IEnumerable<JaegerTag> ToJaegerSpanTags(
        this IEnumerable<LiteDBAttribute> spanAttributes,
        LiteDBSpan span)
    {
        if (span.StatusCode == LiteDBSpanStatusCode.Error)
        {
            yield return new JaegerTag { Key = "error", Type = JaegerTagType.Bool, Value = true };
        }

        yield return new JaegerTag
        {
            Key = "span.kind", Type = JaegerTagType.String, Value = span.SpanKind.ToJaegerSpanKind()
        };

        foreach (var attribute in spanAttributes)
        {
            yield return attribute.ToJaegerTag();
        }
    }

    private static JaegerTag ToJaegerTag(this LiteDBAttribute attribute)
    {
        var jaegerTag = new JaegerTag
        {
            Key = attribute.Key,
            Type = attribute.ValueType.ToJaegerTagType(),
            Value = ConvertTagValue(attribute.ValueType, attribute.Value)
        };

        return jaegerTag;
    }

    private static string ToJaegerTagType(this LiteDBAttributeValueType valueType) => valueType switch
    {
        LiteDBAttributeValueType.StringValue => JaegerTagType.String,
        LiteDBAttributeValueType.BoolValue => JaegerTagType.Bool,
        LiteDBAttributeValueType.IntValue => JaegerTagType.Int64,
        LiteDBAttributeValueType.DoubleValue => JaegerTagType.Float64,
        // TODO: ArrayValue, KvlistValue, BytesValue
        LiteDBAttributeValueType.ArrayValue => JaegerTagType.String,
        LiteDBAttributeValueType.KvlistValue => JaegerTagType.String,
        LiteDBAttributeValueType.BytesValue => JaegerTagType.String,
        _ => throw new ArgumentOutOfRangeException()
    };


    private static IEnumerable<JaegerSpanLog> ToJaegerSpanLogs(
        this IEnumerable<LiteDBSpanEvent> spanEvents)
    {
        foreach (var spanEvent in spanEvents)
        {
            var jaegerSpanLog = new JaegerSpanLog
            {
                Timestamp = spanEvent.TimestampUnixNano / 1000,
                Fields = spanEvent.Attributes.Select(a => new JaegerTag
                {
                    Key = a.Key,
                    Type = a.ValueType.ToJaegerTagType(),
                    Value = ConvertTagValue(a.ValueType, a.Value)
                }).ToArray()
            };

            yield return jaegerSpanLog;
        }
    }

    private static string ToJaegerSpanKind(this LiteDBSpanKind spanKind) => spanKind switch
    {
        LiteDBSpanKind.Internal => JaegerSpanKind.Internal,
        LiteDBSpanKind.Server => JaegerSpanKind.Server,
        LiteDBSpanKind.Client => JaegerSpanKind.Client,
        LiteDBSpanKind.Producer => JaegerSpanKind.Producer,
        LiteDBSpanKind.Consumer => JaegerSpanKind.Consumer,
        _ => JaegerSpanKind.Unspecified
    };

    private static object ConvertTagValue(this LiteDBAttributeValueType valueType, string value) => valueType switch
    {
        LiteDBAttributeValueType.StringValue => value,
        LiteDBAttributeValueType.BoolValue => bool.Parse(value),
        LiteDBAttributeValueType.IntValue => long.Parse(value),
        LiteDBAttributeValueType.DoubleValue => double.Parse(value),
        // TODO: ArrayValue, KvlistValue, BytesValue
        LiteDBAttributeValueType.ArrayValue => value,
        LiteDBAttributeValueType.KvlistValue => value,
        LiteDBAttributeValueType.BytesValue => value,
        _ => throw new ArgumentOutOfRangeException()
    };
}
