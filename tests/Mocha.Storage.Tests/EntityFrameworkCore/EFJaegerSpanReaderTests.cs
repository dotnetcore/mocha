// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Core.Extensions;
using Mocha.Core.Storage.Jaeger;
using Mocha.Core.Storage.Jaeger.Trace;
using Mocha.Storage.EntityFrameworkCore;
using Mocha.Storage.EntityFrameworkCore.Trace;

namespace Mocha.Storage.Tests.EntityFrameworkCore;

public class EFJaegerSpanReaderTests : IDisposable
{
    private readonly IDbContextFactory<MochaContext> _dbContextFactory;
    private readonly IJaegerSpanReader _jaegerSpanReader;
    private readonly ServiceProvider _serviceProvider;

    public EFJaegerSpanReaderTests()
    {
        var services = new ServiceCollection();
        services.AddStorage(builder =>
        {
            builder.UseEntityFrameworkCore(options => { options.UseInMemoryDatabase(Guid.NewGuid().ToString()); });
        });
        _serviceProvider = services.BuildServiceProvider();
        _jaegerSpanReader = _serviceProvider.GetRequiredService<IJaegerSpanReader>();
        _dbContextFactory = _serviceProvider.GetRequiredService<IDbContextFactory<MochaContext>>();
    }

    [Fact]
    public async Task GetServicesAsync()
    {
        var spans = new[]
        {
            new EFSpan
            {
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                SpanName = "SpanName1",
                ParentSpanId = "ParentSpanId1",
                StartTimeUnixNano = 0,
                EndTimeUnixNano = 0,
                DurationNanoseconds = 0,
                StatusCode = null,
                StatusMessage = null,
                SpanKind = EFSpanKind.Unspecified,
                ServiceName = "ServiceName1",
                ServiceInstanceId = "ServiceInstanceId1",
                TraceFlags = 0,
                TraceState = "TraceState1"
            },
            new EFSpan
            {
                TraceId = "TraceId2",
                SpanId = "SpanId2",
                SpanName = "SpanName2",
                ParentSpanId = "ParentSpanId2",
                StartTimeUnixNano = 0,
                EndTimeUnixNano = 0,
                DurationNanoseconds = 0,
                StatusCode = null,
                StatusMessage = null,
                SpanKind = EFSpanKind.Unspecified,
                ServiceName = "ServiceName2",
                ServiceInstanceId = "ServiceInstanceId2",
                TraceFlags = 0,
                TraceState = "TraceState2"
            }
        };

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.Spans.AddRangeAsync(spans);
        await context.SaveChangesAsync();

        var services = await _jaegerSpanReader.GetServicesAsync();
        Assert.Equal(new[] { "ServiceName1", "ServiceName2" }, services);
    }

    [Fact]
    public async Task GetOperationsAsync()
    {
        var spans = new[]
        {
            new EFSpan
            {
                Id = 1,
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                SpanName = "SpanName1",
                ParentSpanId = "ParentSpanId1",
                StartTimeUnixNano = 0,
                EndTimeUnixNano = 0,
                DurationNanoseconds = 0,
                StatusCode = null,
                StatusMessage = null,
                SpanKind = EFSpanKind.Unspecified,
                ServiceName = "ServiceName1",
                ServiceInstanceId = "ServiceInstanceId1",
                TraceFlags = 0,
                TraceState = "TraceState1"
            },
            new EFSpan
            {
                Id = 2,
                TraceId = "TraceId2",
                SpanId = "SpanId2",
                SpanName = "SpanName2",
                ParentSpanId = "ParentSpanId2",
                StartTimeUnixNano = 0,
                EndTimeUnixNano = 0,
                DurationNanoseconds = 0,
                StatusCode = null,
                StatusMessage = null,
                SpanKind = EFSpanKind.Unspecified,
                ServiceName = "ServiceName2",
                ServiceInstanceId = "ServiceInstanceId2",
                TraceFlags = 0,
                TraceState = "TraceState2"
            }
        };

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.Spans.AddRangeAsync(spans);
        await context.SaveChangesAsync();

        var operations = await _jaegerSpanReader.GetOperationsAsync("ServiceName1");
        Assert.Equal(new[] { "SpanName1" }, operations);
    }

    [Fact]
    public async Task FindTracesAsync_JaegerTraceQueryParameters()
    {
        var now = DateTimeOffset.Now;
        var efSpans = new List<EFSpan>
        {
            new()
            {
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
                TraceId = "TraceId1",
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

        var efResourceAttributes = new List<EFResourceAttribute>
        {
            new()
            {
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                Key = "service.name",
                ValueType = EFAttributeValueType.StringValue,
                Value = "ServiceName1"
            }
        };

        var efSpanAttributes = new List<EFSpanAttribute>
        {
            new()
            {
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                Key = "SpanAttributeKey1",
                ValueType = EFAttributeValueType.StringValue,
                Value = "SpanAttributeValue1"
            },
            new()
            {
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                Key = "SpanAttributeKey2",
                ValueType = EFAttributeValueType.BoolValue,
                Value = "True"
            },
            new()
            {
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                Key = "SpanAttributeKey3",
                ValueType = EFAttributeValueType.IntValue,
                Value = "31"
            },
            new()
            {
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                Key = "SpanAttributeKey4",
                ValueType = EFAttributeValueType.DoubleValue,
                Value = "11.1"
            }
        };

        var efSpanEvents = new List<EFSpanEvent>
        {
            new()
            {
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                Index = 0,
                Name = "EventName1",
                TimestampUnixNano = now.AddMinutes(-1).ToUnixTimeNanoseconds()
            },
            new()
            {
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                Index = 1,
                Name = "EventName2",
                TimestampUnixNano = now.AddMinutes(-1).ToUnixTimeNanoseconds()
            },
            new()
            {
                TraceId = "TraceId1",
                SpanId = "SpanId2",
                Index = 0,
                Name = "EventName3",
                TimestampUnixNano = now.AddMinutes(-1).ToUnixTimeNanoseconds()
            }
        };

        var efSpanEventAttributes = new List<EFSpanEventAttribute>
        {
            new()
            {
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                SpanEventIndex = 0,
                Key = "EventAttributeKey1",
                ValueType = EFAttributeValueType.StringValue,
                Value = "EventAttributeValue1"
            },
            new()
            {
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                SpanEventIndex = 0,
                Key = "EventAttributeKey2",
                ValueType = EFAttributeValueType.BoolValue,
                Value = "True"
            },
            new()
            {
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                SpanEventIndex = 0,
                Key = "EventAttributeKey3",
                ValueType = EFAttributeValueType.IntValue,
                Value = "31"
            },
            new()
            {
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                SpanEventIndex = 0,
                Key = "EventAttributeKey4",
                ValueType = EFAttributeValueType.DoubleValue,
                Value = "11.1"
            },
            new()
            {
                TraceId = "TraceId1",
                SpanId = "SpanId1",
                SpanEventIndex = 1,
                Key = "EventAttributeKey1",
                ValueType = EFAttributeValueType.StringValue,
                Value = "EventAttributeValue1"
            }
        };

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.Spans.AddRangeAsync(efSpans);
        await context.ResourceAttributes.AddRangeAsync(efResourceAttributes);
        await context.SpanAttributes.AddRangeAsync(efSpanAttributes);
        await context.SpanEvents.AddRangeAsync(efSpanEvents);
        await context.SpanEventAttributes.AddRangeAsync(efSpanEventAttributes);
        await context.SaveChangesAsync();

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
            StartTimeMinUnixNano = now.AddMinutes(-2).ToUnixTimeNanoseconds(),
            StartTimeMaxUnixNano = now.AddMinutes(2).ToUnixTimeNanoseconds(),
            DurationMinNanoseconds = 60_000_000_000,
            DurationMaxNanoseconds = 120_000_000_000,
            NumTraces = 10
        };

        var traces = await _jaegerSpanReader.FindTracesAsync(queryParameters);
        var trace = traces.Single();
        var span = trace.Spans.Single();
        var process = trace.Processes.Single();

        Assert.Equal("TraceId1", trace.TraceID);
        Assert.Equal("SpanId1", span.SpanID);
        Assert.Equal("SpanName1", span.OperationName);
        Assert.Equal("ServiceName1", process.Value.ServiceName);
        Assert.Equivalent(new JaegerTag { Key = "span.kind", Type = JaegerTagType.String, Value = "server" },
            span.Tags.Single(t => t.Key == "span.kind"));
    }

    [Fact]
    public async Task FindTracesAsync_TraceID()
    {
        var now = DateTimeOffset.Now;
        var efSpans = new List<EFSpan>
        {
            new()
            {
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
                TraceId = "TraceId1",
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

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.Spans.AddRangeAsync(efSpans);
        await context.SaveChangesAsync();

        var traces = await _jaegerSpanReader.FindTracesAsync(
            ["TraceId1"],
            now.AddMinutes(-2).ToUnixTimeNanoseconds(),
            now.AddMinutes(2).ToUnixTimeNanoseconds());
        Assert.Single(traces);
        Assert.Equal("TraceId1", traces.First().TraceID);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }
}
