// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Globalization;
using Google.Protobuf;
using Mocha.Core.Extensions;
using Mocha.Core.Models.Trace;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Resource.V1;
using OpenTelemetry.Proto.Trace.V1;
using Status = OpenTelemetry.Proto.Trace.V1.Status;

namespace Mocha.Core.Tests.Conversions;

public class OTelToMochaSpanConversionTests
{
    [Fact]
    public void ConvertSpan()
    {
        var now = DateTimeOffset.Now;
        var resource = new Resource
        {
            Attributes =
            {
                new List<KeyValue>
                {
                    new()
                    {
                        Key = "service.name", Value = new AnyValue { StringValue = "TestServiceName" },
                    },
                    new()
                    {
                        Key = "service.instance.id",
                        Value = new AnyValue { StringValue = "TestServiceInstanceId" },
                    },
                    new()
                    {
                        Key = "service.version",
                        Value = new AnyValue { StringValue = "TestServiceVersion" },
                    }
                }
            }
        };

        var oTelSpan = new Span
        {
            TraceId = ConvertTraceIdToByteString("5ae111ddc72d3fea3c6e4501961d8c8a"),
            SpanId = ConvertSpanIdToByteString("b10497b337748713"),
            TraceState = "TestTraceState",
            ParentSpanId = ConvertSpanIdToByteString("ef5b92deb45ee2f9"),
            Flags = 1,
            Name = "TestSpan",
            Kind = Span.Types.SpanKind.Server,
            StartTimeUnixNano = now.AddMinutes(-1).ToUnixTimeNanoseconds(),
            EndTimeUnixNano = now.AddMinutes(1).ToUnixTimeNanoseconds(),
            Status = new Status { Code = Status.Types.StatusCode.Ok, Message = "TestStatusMessage" },
            Attributes =
            {
                new List<KeyValue>
                {
                    new() { Key = "SpanAttributeKey1", Value = new AnyValue { StringValue = "SpanAttributeValue1" } },
                    new() { Key = "SpanAttributeKey2", Value = new AnyValue { BoolValue = true } },
                    new() { Key = "SpanAttributeKey3", Value = new AnyValue { IntValue = 3 } },
                    new() { Key = "SpanAttributeKey4", Value = new AnyValue { DoubleValue = 1.1 } }
                }
            },
            Events =
            {
                new List<Span.Types.Event>
                {
                    new()
                    {
                        TimeUnixNano = now.AddMinutes(-1).ToUnixTimeNanoseconds(),
                        Name = "TestEvent1",
                        Attributes =
                        {
                            new List<KeyValue>
                            {
                                new()
                                {
                                    Key = "EventAttributeKey1",
                                    Value = new AnyValue { StringValue = "EventAttributeValue1" }
                                },
                                new() { Key = "EventAttributeKey2", Value = new AnyValue { BoolValue = true } },
                                new() { Key = "EventAttributeKey3", Value = new AnyValue { IntValue = 31 } },
                                new() { Key = "EventAttributeKey4", Value = new AnyValue { DoubleValue = 11.1 } },
                            }
                        }
                    }
                }
            },
            Links =
            {
                new Span.Types.Link
                {
                    TraceId = ConvertTraceIdToByteString("7ae111ddc72d3fea3c6e4501961d8c8a"),
                    SpanId = ConvertSpanIdToByteString("a10497b337748713"),
                    TraceState = "TestTraceState",
                    Flags = 1,
                    Attributes =
                    {
                        new List<KeyValue>
                        {
                            new()
                            {
                                Key = "LinkAttributeKey1",
                                Value = new AnyValue { StringValue = "LinkAttributeValue1" }
                            },
                        }
                    }
                }
            },
        };

        var mochaSpan = oTelSpan.ToMochaSpan(resource);
        var mochaResource = mochaSpan.Resource;
        var mochaSpanAttributes = mochaSpan.Attributes;
        var mochaSpanEvents = mochaSpan.Events;
        var mochaSpanLinks = mochaSpan.Links;

        var expectResource = new MochaResource
        {
            ServiceName = "TestServiceName",
            ServiceInstanceId = "TestServiceInstanceId",
            Attributes = new List<MochaAttribute>
            {
                new()
                {
                    Key = "service.version",
                    ValueType = MochaAttributeValueType.StringValue,
                    Value = "TestServiceVersion"
                }
            }
        };

        var expectSpanAttributes = new List<MochaAttribute>
        {
            new()
            {
                Key = "SpanAttributeKey1",
                ValueType = MochaAttributeValueType.StringValue,
                Value = "SpanAttributeValue1"
            },
            new() { Key = "SpanAttributeKey2", ValueType = MochaAttributeValueType.BoolValue, Value = "True" },
            new() { Key = "SpanAttributeKey3", ValueType = MochaAttributeValueType.IntValue, Value = "3" },
            new() { Key = "SpanAttributeKey4", ValueType = MochaAttributeValueType.DoubleValue, Value = "1.1" }
        };

        var expectSpanEvents = new List<MochaSpanEvent>
        {
            new()
            {
                Name = "TestEvent1",
                TimestampUnixNano = now.AddMinutes(-1).ToUnixTimeNanoseconds(),
                Attributes = new List<MochaAttribute>
                {
                    new()
                    {
                        Key = "EventAttributeKey1",
                        ValueType = MochaAttributeValueType.StringValue,
                        Value = "EventAttributeValue1"
                    },
                    new()
                    {
                        Key = "EventAttributeKey2",
                        ValueType = MochaAttributeValueType.BoolValue,
                        Value = "True"
                    },
                    new()
                    {
                        Key = "EventAttributeKey3",
                        ValueType = MochaAttributeValueType.IntValue,
                        Value = "31"
                    },
                    new()
                    {
                        Key = "EventAttributeKey4",
                        ValueType = MochaAttributeValueType.DoubleValue,
                        Value = "11.1"
                    }
                }
            }
        };

        var expectSpanLinks = new List<MochaSpanLink>
        {
            new()
            {
                LinkedTraceId = "7ae111ddc72d3fea3c6e4501961d8c8a",
                LinkedSpanId = "a10497b337748713",
                LinkedTraceState = "TestTraceState",
                LinkedTraceFlags = 1,
                Attributes = new List<MochaAttribute>
                {
                    new()
                    {
                        Key = "LinkAttributeKey1",
                        ValueType = MochaAttributeValueType.StringValue,
                        Value = "LinkAttributeValue1"
                    }
                }
            }
        };

        Assert.Equivalent(expectResource, mochaResource);
        Assert.Equivalent(expectSpanAttributes, mochaSpanAttributes);
        Assert.Equivalent(expectSpanEvents, mochaSpanEvents);
        Assert.Equivalent(expectSpanLinks, mochaSpanLinks);
        Assert.Equivalent(
            new MochaSpan
            {
                TraceId = "5ae111ddc72d3fea3c6e4501961d8c8a",
                SpanId = "b10497b337748713",
                SpanName = "TestSpan",
                ParentSpanId = "ef5b92deb45ee2f9",
                StartTimeUnixNano = now.AddMinutes(-1).ToUnixTimeNanoseconds(),
                EndTimeUnixNano = now.AddMinutes(1).ToUnixTimeNanoseconds(),
                DurationNanoseconds =
                    now.AddMinutes(1).ToUnixTimeNanoseconds() - now.AddMinutes(-1).ToUnixTimeNanoseconds(),
                StatusCode = MochaSpanStatusCode.Ok,
                StatusMessage = "TestStatusMessage",
                SpanKind = MochaSpanKind.Server,
                Resource = expectResource,
                TraceFlags = 1,
                TraceState = "TestTraceState",
                Attributes = expectSpanAttributes,
                Events = expectSpanEvents,
                Links = expectSpanLinks
            }, mochaSpan);
    }

    private static ByteString ConvertSpanIdToByteString(string spanId)
    {
        if (string.IsNullOrWhiteSpace(spanId))
        {
            return ByteString.Empty;
        }

        var bytes = ConvertLongToBytes(long.Parse(spanId, NumberStyles.HexNumber));
        return ByteString.CopyFrom(bytes);
    }

    private static ByteString ConvertTraceIdToByteString(string traceId)
    {
        if (string.IsNullOrWhiteSpace(traceId))
        {
            return ByteString.Empty;
        }

        var high = long.Parse(traceId[..16], NumberStyles.HexNumber);
        var low = long.Parse(traceId[16..], NumberStyles.HexNumber);
        var bytes = ConvertLongToBytes(high).Concat(ConvertLongToBytes(low)).ToArray();
        return ByteString.CopyFrom(bytes);
    }

    private static byte[] ConvertLongToBytes(long value) =>
        BitConverter.IsLittleEndian
            ? BitConverter.GetBytes(value).Reverse().ToArray()
            : BitConverter.GetBytes(value);
}
