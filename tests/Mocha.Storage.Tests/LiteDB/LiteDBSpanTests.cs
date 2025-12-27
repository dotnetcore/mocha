// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Core.Extensions;
using Mocha.Core.Models.Trace;
using Mocha.Core.Storage;
using Mocha.Core.Storage.Jaeger;
using Mocha.Core.Storage.Jaeger.Trace;
using Mocha.Storage.LiteDB.Trace;

namespace Mocha.Storage.Tests.LiteDB;

public class LiteDBSpanTests : IDisposable
{
    private readonly TempDatabasePath _tempDatabasePath;
    private readonly ServiceProvider _serviceProvider;

    private readonly ITelemetryDataWriter<MochaSpan> _writer;
    private readonly IJaegerSpanReader _reader;

    public LiteDBSpanTests()
    {
        _tempDatabasePath = TempDatabasePath.Create();
        var services = new ServiceCollection();

        services.AddStorage()
            .WithTracing(options =>
            {
                options.UseLiteDB(liteDbOptions => { liteDbOptions.DatabasePath = _tempDatabasePath.Path; });
            });

        _serviceProvider = services.BuildServiceProvider();

        _writer = _serviceProvider.GetRequiredService<ITelemetryDataWriter<MochaSpan>>();
        _reader = _serviceProvider.GetRequiredService<IJaegerSpanReader>();
    }

    [Fact]
    public async Task FindTracesAsync()
    {
        var now = DateTimeOffset.Now;
        var mochaSpans = new List<MochaSpan>
        {
            new MochaSpan
            {
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                SpanName = "SpanName1",
                ParentSpanId = "ParentSpanId1",
                StartTimeUnixNano = now.ToUnixTimeNanoseconds(),
                EndTimeUnixNano = now.AddMinutes(1).ToUnixTimeNanoseconds(),
                DurationNanoseconds = 60_000_000_000,
                StatusCode = MochaSpanStatusCode.Ok,
                StatusMessage = "StatusMessage1",
                SpanKind = MochaSpanKind.Server,
                Resource = new MochaResource
                {
                    ServiceName = "ServiceName1",
                    ServiceInstanceId = "ServiceInstanceId1",
                    Attributes =
                    [
                        new MochaAttribute
                        {
                            Key = "service.instance.id",
                            ValueType = MochaAttributeValueType.StringValue,
                            Value = "ServiceInstanceId1"
                        },
                        new MochaAttribute
                        {
                            Key = "ServiceVersion",
                            ValueType = MochaAttributeValueType.StringValue,
                            Value = "ServiceVersion1"
                        }
                    ]
                },
                TraceFlags = 1,
                TraceState = "TraceState1",
                Links =
                [
                    new MochaSpanLink
                    {
                        LinkedTraceId = "LinkedTraceId1",
                        LinkedSpanId = "LinkedSpanId1",
                        LinkedTraceState = "LinkedTraceState1",
                        LinkedTraceFlags = 1,
                        Attributes =
                        [
                            new MochaAttribute
                            {
                                Key = "LinkAttributeKey1",
                                ValueType = MochaAttributeValueType.StringValue,
                                Value = "LinkAttributeValue1"
                            },
                            new MochaAttribute
                            {
                                Key = "LinkAttributeKey2",
                                ValueType = MochaAttributeValueType.IntValue,
                                Value = "21"
                            },
                        ]
                    }
                ],
                Attributes =
                [
                    new MochaAttribute
                    {
                        Key = "SpanAttributeKey1",
                        ValueType = MochaAttributeValueType.StringValue,
                        Value = "SpanAttributeValue1"
                    },
                    new MochaAttribute
                    {
                        Key = "SpanAttributeKey2", ValueType = MochaAttributeValueType.BoolValue, Value = "True"
                    },
                    new MochaAttribute
                    {
                        Key = "SpanAttributeKey3", ValueType = MochaAttributeValueType.IntValue, Value = "31"
                    },
                    new MochaAttribute
                    {
                        Key = "SpanAttributeKey4", ValueType = MochaAttributeValueType.DoubleValue, Value = "11.1"
                    }
                ],
                Events =
                [
                    new MochaSpanEvent
                    {
                        Name = "EventName1",
                        TimestampUnixNano = now.AddMinutes(-1).ToUnixTimeNanoseconds(),
                        Attributes =
                        [
                            new MochaAttribute
                            {
                                Key = "EventAttributeKey1",
                                ValueType = MochaAttributeValueType.StringValue,
                                Value = "EventAttributeValue1"
                            },
                            new MochaAttribute
                            {
                                Key = "EventAttributeKey2",
                                ValueType = MochaAttributeValueType.BoolValue,
                                Value = "True"
                            },
                            new MochaAttribute
                            {
                                Key = "EventAttributeKey3",
                                ValueType = MochaAttributeValueType.IntValue,
                                Value = "31"
                            },
                            new MochaAttribute
                            {
                                Key = "EventAttributeKey4",
                                ValueType = MochaAttributeValueType.DoubleValue,
                                Value = "11.1"
                            }
                        ]
                    }
                ]
            },
            new MochaSpan
            {
                TraceId = "TraceId2",
                SpanId = "SpanId2",
                SpanName = "SpanName2",
                ParentSpanId = "ParentSpanId2",
                StartTimeUnixNano = now.ToUnixTimeNanoseconds(),
                EndTimeUnixNano = now.AddMinutes(2).ToUnixTimeNanoseconds(),
                DurationNanoseconds = 120_000_000_000,
                StatusCode = MochaSpanStatusCode.Error,
                StatusMessage = "StatusMessage2",
                SpanKind = MochaSpanKind.Client,
                Resource = new MochaResource
                {
                    ServiceName = "ServiceName2",
                    ServiceInstanceId = "ServiceInstanceId2",
                    Attributes =
                    [
                        new MochaAttribute
                        {
                            Key = "ServiceVersion",
                            ValueType = MochaAttributeValueType.StringValue,
                            Value = "ServiceVersion2"
                        }
                    ]
                },
                TraceFlags = 1,
                TraceState = "TraceState2",
                Links = [],
                Attributes = [],
                Events =
                [
                    new MochaSpanEvent
                    {
                        Name = "EventName3",
                        TimestampUnixNano = now.AddMinutes(-1).ToUnixTimeNanoseconds(),
                        Attributes = []
                    }
                ]
            }
        };

        await _writer.WriteAsync(mochaSpans);

        var queryParameters = new JaegerTraceQueryParameters
        {
            ServiceName = "ServiceName1",
            OperationName = "SpanName1",
            Tags = new Dictionary<string, object>
            {
                { "SpanAttributeKey1", "SpanAttributeValue1" },
                { "SpanAttributeKey2", true },
                { "SpanAttributeKey3", 31 },
                { "SpanAttributeKey4", 11.1 }
            },
            StartTimeMinUnixNano = now.AddMinutes(-5).ToUnixTimeNanoseconds(),
            StartTimeMaxUnixNano = now.AddMinutes(5).ToUnixTimeNanoseconds(),
            DurationMinNanoseconds = 50_000_000_000,
            DurationMaxNanoseconds = 70_000_000_000,
            NumTraces = 10
        };

        var traces = await _reader.FindTracesAsync(queryParameters);

        Assert.Single(traces);
        var trace = traces.First();
        Assert.Equal("TraceId1", trace.TraceID);
        Assert.Single(trace.Spans);
        var span = trace.Spans.First();
        Assert.Equal("SpanId1", span.SpanID);

        span.Tags.Should().BeEquivalentTo(new List<JaegerTag>
        {
            new JaegerTag
            {
                Key = "span.kind",
                Type = JaegerTagType.String,
                Value = "server"
            },
            new JaegerTag
            {
                Key = "SpanAttributeKey1",
                Type = JaegerTagType.String,
                Value = "SpanAttributeValue1"
            },
            new JaegerTag
            {
                Key = "SpanAttributeKey2",
                Type = JaegerTagType.Bool,
                Value = true
            },
            new JaegerTag
            {
                Key = "SpanAttributeKey3",
                Type = JaegerTagType.Int64,
                Value = 31L
            },
            new JaegerTag
            {
                Key = "SpanAttributeKey4",
                Type = JaegerTagType.Float64,
                Value = 11.1
            }
        });

        trace.Processes.Should().BeEquivalentTo(new Dictionary<string, JaegerProcess>
        {
            {
                "ServiceInstanceId1", new JaegerProcess
                {
                    ServiceName = "ServiceName1",
                    Tags =
                    [
                        new JaegerTag
                        {
                            Key = "service.instance.id",
                            Type = JaegerTagType.String,
                            Value = "ServiceInstanceId1"
                        },
                        new JaegerTag
                        {
                            Key = "ServiceVersion",
                            Type = JaegerTagType.String,
                            Value = "ServiceVersion1"
                        }
                    ],
                    ProcessID = "ServiceInstanceId1"
                }
            }
        });
    }

    public void Dispose()
    {
        _tempDatabasePath.Dispose();
        _serviceProvider.Dispose();
    }
}
