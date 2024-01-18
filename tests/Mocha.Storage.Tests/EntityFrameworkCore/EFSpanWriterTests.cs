// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Core.Extensions;
using Mocha.Core.Models.Trace;
using Mocha.Core.Storage;
using Mocha.Storage.EntityFrameworkCore;
using Mocha.Storage.EntityFrameworkCore.Trace;

namespace Mocha.Storage.Tests.EntityFrameworkCore;

public class EFSpanWriterTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ISpanWriter _spanWriter;

    public EFSpanWriterTests()
    {
        var services = new ServiceCollection();
        services.AddStorage(builder =>
        {
            builder.UseEntityFrameworkCore(options =>
            {
                options.UseInMemoryDatabase(Guid.NewGuid().ToString());
            });
        });
        _serviceProvider = services.BuildServiceProvider();
        _spanWriter = _serviceProvider.GetRequiredService<ISpanWriter>();
    }

    [Fact]
    public async Task WriteSpanAsync()
    {
        var now = DateTimeOffset.Now;
        var spans = new[]
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
                Resource =
                    new MochaResource
                    {
                        ServiceName = "ServiceName1",
                        ServiceInstanceId = "ServiceInstanceId1",
                        Attributes =
                            new[]
                            {
                                new MochaAttribute
                                {
                                    Key = "ServiceVersion",
                                    ValueType = MochaAttributeValueType.StringValue,
                                    Value = "ServiceVersion1"
                                }
                            }
                    },
                TraceFlags = 1,
                TraceState = "TraceState1",
                Attributes =
                    new[]
                    {
                        new MochaAttribute
                        {
                            Key = "SpanAttributeKey1",
                            ValueType = MochaAttributeValueType.StringValue,
                            Value = "SpanAttributeValue1"
                        },
                        new MochaAttribute
                        {
                            Key = "SpanAttributeKey2",
                            ValueType = MochaAttributeValueType.BoolValue,
                            Value = "True"
                        },
                        new MochaAttribute
                        {
                            Key = "SpanAttributeKey3",
                            ValueType = MochaAttributeValueType.IntValue,
                            Value = "31"
                        },
                        new MochaAttribute
                        {
                            Key = "SpanAttributeKey4",
                            ValueType = MochaAttributeValueType.DoubleValue,
                            Value = "11.1"
                        }
                    },
                Events = new[]
                {
                    new MochaSpanEvent
                    {
                        Name = "EventName1",
                        TimestampUnixNano = now.AddMinutes(-1).ToUnixTimeNanoseconds(),
                        Attributes =
                            new[]
                            {
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
                            }
                    },
                    new MochaSpanEvent
                    {
                        Name = "EventName2",
                        TimestampUnixNano = now.AddMinutes(-1).ToUnixTimeNanoseconds(),
                        Attributes = new[]
                        {
                            new MochaAttribute
                            {
                                Key = "EventAttributeKey1",
                                ValueType = MochaAttributeValueType.StringValue,
                                Value = "EventAttributeValue1"
                            }
                        }
                    }
                },
                Links =
                    new[]
                    {
                        new MochaSpanLink
                        {
                            LinkedTraceId = "LinkedTraceId1",
                            LinkedSpanId = "LinkedSpanId1",
                            LinkedTraceState = "LinkedTraceState1",
                            LinkedTraceFlags = 1,
                            Attributes =
                                new[]
                                {
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
                                }
                        },
                        new MochaSpanLink
                        {
                            LinkedTraceId = "LinkedTraceId2",
                            LinkedSpanId = "LinkedSpanId2",
                            LinkedTraceState = "LinkedTraceState2",
                            LinkedTraceFlags = 2,
                            Attributes =
                                new[]
                                {
                                    new MochaAttribute
                                    {
                                        Key = "LinkAttributeKey3",
                                        ValueType = MochaAttributeValueType.BoolValue,
                                        Value = "True"
                                    }
                                }
                        }
                    },
            },
            new MochaSpan
            {
                TraceId = "TraceId2",
                SpanId = "SpanId2",
                SpanName = "SpanName2",
                ParentSpanId = "ParentSpanId2",
                StartTimeUnixNano = now.ToUnixTimeNanoseconds(),
                EndTimeUnixNano = now.AddMinutes(2)
                    .ToUnixTimeNanoseconds(),
                DurationNanoseconds = 120_000_000_000,
                StatusCode = MochaSpanStatusCode.Error,
                StatusMessage = "StatusMessage2",
                SpanKind = MochaSpanKind.Client,
                Resource =
                    new MochaResource
                    {
                        ServiceName = "ServiceName2",
                        ServiceInstanceId = "ServiceInstanceId2",
                        Attributes =
                            new[]
                            {
                                new MochaAttribute
                                {
                                    Key = "ServiceVersion",
                                    ValueType = MochaAttributeValueType.StringValue,
                                    Value = "ServiceVersion2"
                                }
                            }
                    },
                TraceFlags = 1,
                TraceState = "TraceState2",
                Links = Enumerable.Empty<MochaSpanLink>(),
                Attributes = Enumerable.Empty<MochaAttribute>(),
                Events = new[]
                {
                    new MochaSpanEvent
                    {
                        Name = "EventName3",
                        TimestampUnixNano = now.AddMinutes(-1).ToUnixTimeNanoseconds(),
                        Attributes = Enumerable.Empty<MochaAttribute>()
                    }
                }
            }
        };

        await _spanWriter.WriteAsync(spans);

        var dbContextFactory = _serviceProvider.GetRequiredService<IDbContextFactory<MochaContext>>();
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var efSpans = context.Spans.ToList();
        var efResourceAttributes = context.ResourceAttributes.ToList();
        var efSpanAttributes = context.SpanAttributes.ToList();
        var efSpanEvents = context.SpanEvents.ToList();
        var efSpanLinks = context.SpanLinks.ToList();
        var efSpanEventAttributes = context.SpanEventAttributes.ToList();
        var efSpanLinkAttributes = context.SpanLinkAttributes.ToList();

        var expectedSpans = new List<EFSpan>
        {
            new()
            {
                Id = 1,
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                SpanName = "SpanName1",
                ParentSpanId = "ParentSpanId1",
                StartTimeUnixNano = now.ToUnixTimeNanoseconds(),
                EndTimeUnixNano = now.AddMinutes(1).ToUnixTimeNanoseconds(),
                DurationNanoseconds = 60_000_000_000,
                StatusCode = EFSpanStatusCode.Ok,
                StatusMessage = "StatusMessage1",
                SpanKind = EFSpanKind.Server,
                ServiceName = "ServiceName1",
                ServiceInstanceId = "ServiceInstanceId1",
                TraceFlags = 1,
                TraceState = "TraceState1",
            },
            new()
            {
                Id = 2,
                TraceId = "TraceId2",
                SpanId = "SpanId2",
                SpanName = "SpanName2",
                ParentSpanId = "ParentSpanId2",
                StartTimeUnixNano = now.ToUnixTimeNanoseconds(),
                EndTimeUnixNano = now.AddMinutes(2).ToUnixTimeNanoseconds(),
                DurationNanoseconds = 120_000_000_000,
                StatusCode = EFSpanStatusCode.Error,
                StatusMessage = "StatusMessage2",
                SpanKind = EFSpanKind.Client,
                ServiceName = "ServiceName2",
                ServiceInstanceId = "ServiceInstanceId2",
                TraceFlags = 1,
                TraceState = "TraceState2",
            }
        };

        var expectedSpanAttributes = new List<EFSpanAttribute>
        {
            new()
            {
                Id = 1,
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                Key = "SpanAttributeKey1",
                ValueType = EFAttributeValueType.StringValue,
                Value = "SpanAttributeValue1"
            },
            new()
            {
                Id = 2,
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                Key = "SpanAttributeKey2",
                ValueType = EFAttributeValueType.BoolValue,
                Value = "True"
            },
            new()
            {
                Id = 3,
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                Key = "SpanAttributeKey3",
                ValueType = EFAttributeValueType.IntValue,
                Value = "31"
            },
            new()
            {
                Id = 4,
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                Key = "SpanAttributeKey4",
                ValueType = EFAttributeValueType.DoubleValue,
                Value = "11.1"
            }
        };

        var expectedSpanEvents = new List<EFSpanEvent>
        {
            new()
            {
                Id = 1,
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                Index = 0,
                Name = "EventName1",
                TimestampUnixNano = now.AddMinutes(-1).ToUnixTimeNanoseconds()
            },
            new()
            {
                Id = 2,
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                Index = 1,
                Name = "EventName2",
                TimestampUnixNano = now.AddMinutes(-1).ToUnixTimeNanoseconds()
            },
            new()
            {
                Id = 3,
                TraceId = "TraceId2",
                SpanId = "SpanId2",
                Index = 0,
                Name = "EventName3",
                TimestampUnixNano = now.AddMinutes(-1).ToUnixTimeNanoseconds()
            }
        };

        var expectedSpanEventAttributes = new List<EFSpanEventAttribute>
        {
            new()
            {
                Id = 1,
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                SpanEventIndex = 0,
                Key = "EventAttributeKey1",
                ValueType = EFAttributeValueType.StringValue,
                Value = "EventAttributeValue1"
            },
            new()
            {
                Id = 2,
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                SpanEventIndex = 0,
                Key = "EventAttributeKey2",
                ValueType = EFAttributeValueType.BoolValue,
                Value = "True"
            },
            new()
            {
                Id = 3,
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                SpanEventIndex = 0,
                Key = "EventAttributeKey3",
                ValueType = EFAttributeValueType.IntValue,
                Value = "31"
            },
            new()
            {
                Id = 4,
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                SpanEventIndex = 0,
                Key = "EventAttributeKey4",
                ValueType = EFAttributeValueType.DoubleValue,
                Value = "11.1"
            },
            new()
            {
                Id = 5,
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                SpanEventIndex = 1,
                Key = "EventAttributeKey1",
                ValueType = EFAttributeValueType.StringValue,
                Value = "EventAttributeValue1"
            }
        };

        var expectedSpanLinks = new List<EFSpanLink>
        {
            new()
            {
                Id = 1,
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                Index = 0,
                LinkedTraceId = "LinkedTraceId1",
                LinkedSpanId = "LinkedSpanId1",
                LinkedTraceState = "LinkedTraceState1",
                LinkedTraceFlags = 1
            },
            new()
            {
                Id = 2,
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                Index = 1,
                LinkedTraceId = "LinkedTraceId2",
                LinkedSpanId = "LinkedSpanId2",
                LinkedTraceState = "LinkedTraceState2",
                LinkedTraceFlags = 2
            }
        };

        var expectedSpanLinkAttributes = new List<EFSpanLinkAttribute>
        {
            new()
            {
                Id = 1,
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                SpanLinkIndex = 0,
                Key = "LinkAttributeKey1",
                ValueType = EFAttributeValueType.StringValue,
                Value = "LinkAttributeValue1"
            },
            new()
            {
                Id = 2,
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                SpanLinkIndex = 0,
                Key = "LinkAttributeKey2",
                ValueType = EFAttributeValueType.IntValue,
                Value = "21"
            },
            new()
            {
                Id = 3,
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                SpanLinkIndex = 1,
                Key = "LinkAttributeKey3",
                ValueType = EFAttributeValueType.BoolValue,
                Value = "True"
            }
        };

        Assert.Equal(2, efSpans.Count);
        Assert.Equal(2, efResourceAttributes.Count);
        Assert.Equal(4, efSpanAttributes.Count);
        Assert.Equal(3, efSpanEvents.Count);
        Assert.Equal(2, efSpanLinks.Count);
        Assert.Equal(5, efSpanEventAttributes.Count);
        Assert.Equal(3, efSpanLinkAttributes.Count);

        for (var i = 0; i < efSpans.Count; i++)
        {
            Assert.Equivalent(expectedSpans[i], efSpans[i]);
        }

        for (var i = 0; i < efSpanAttributes.Count; i++)
        {
            Assert.Equivalent(expectedSpanAttributes[i], efSpanAttributes[i]);
        }

        for (var i = 0; i < efSpanEvents.Count; i++)
        {
            Assert.Equivalent(expectedSpanEvents[i], efSpanEvents[i]);
        }

        for (var i = 0; i < efSpanEventAttributes.Count; i++)
        {
            Assert.Equivalent(expectedSpanEventAttributes[i], efSpanEventAttributes[i]);
        }

        for (var i = 0; i < efSpanLinks.Count; i++)
        {
            Assert.Equivalent(expectedSpanLinks[i], efSpanLinks[i]);
        }

        for (var i = 0; i < efSpanLinkAttributes.Count; i++)
        {
            Assert.Equivalent(expectedSpanLinkAttributes[i], efSpanLinkAttributes[i]);
        }
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }
}
