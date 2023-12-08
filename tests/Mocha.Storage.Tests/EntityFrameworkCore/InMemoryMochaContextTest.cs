// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Diagnostics;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Core.Storage;
using Mocha.Storage.EntityFrameworkCore;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Trace.V1;
using Span = OpenTelemetry.Proto.Trace.V1.Span;

namespace Mocha.Storage.Tests.EntityFrameworkCore;

public class InMemoryMochaContextTest
{
    private readonly DbContextOptions<MochaContext> _contextOptions;
    private readonly IServiceCollection _serviceCollection;

    public InMemoryMochaContextTest()
    {
        _serviceCollection = new ServiceCollection();
        _contextOptions = new DbContextOptionsBuilder<MochaContext>()
            .UseInMemoryDatabase("InMemoryMochaContextTest")
            .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _serviceCollection.AddStorage(x =>
        {
            x.UseEntityFrameworkCore(context =>
            {
                context.UseInMemoryDatabase($"InMemoryMochaContextTest{Guid.NewGuid().ToString()}")
                    .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });
        });
    }

    [Fact]
    public async Task CreateDatabase()
    {
        await using var context = new MochaContext(_contextOptions);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }


    [Fact]
    public async Task EntityFrameworkSpanWriterAsync()
    {
        var provider = _serviceCollection.BuildServiceProvider();
        var context = provider.GetRequiredService<MochaContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        var entityFrameworkSpanWriter = provider.GetRequiredService<ISpanWriter>();
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var traceIdBytes = new byte[16];
        var spanIdBytes = new byte[8];
        traceId.CopyTo(traceIdBytes);
        spanId.CopyTo(spanIdBytes);
        ByteString parentSpanIdString;
        var parentSpanIdBytes = new byte[8];
        ActivitySpanId.CreateRandom().CopyTo(parentSpanIdBytes);
        parentSpanIdString = UnsafeByteOperations.UnsafeWrap(parentSpanIdBytes);

        var span = new Span()
        {
            Name = "Http",
            Kind = Span.Types.SpanKind.Client,
            TraceId = UnsafeByteOperations.UnsafeWrap(traceIdBytes),
            SpanId = UnsafeByteOperations.UnsafeWrap(spanIdBytes),
            ParentSpanId = parentSpanIdString,
            TraceState = "string.Empty",
            StartTimeUnixNano = (ulong)DateTimeOffset.UtcNow.UtcTicks,
            EndTimeUnixNano = (ulong)DateTimeOffset.UtcNow.UtcTicks,
            Status = new Status() { Message = "Success", Code = Status.Types.StatusCode.Ok, },
        };
        span.Links.Add(new Span.Types.Link()
        {
            TraceId = UnsafeByteOperations.UnsafeWrap(traceIdBytes),
            SpanId = UnsafeByteOperations.UnsafeWrap(spanIdBytes),
            TraceState = "",
            Flags = 1,
        });
        span.Events.Add(
            new Span.Types.Event() { Name = "mysql", TimeUnixNano = (ulong)DateTimeOffset.UtcNow.UtcTicks, });
        span.Attributes.Add(new KeyValue()
        {
            Key = "http.url",
            Value = new AnyValue() { StringValue = "https://github.com/open-telemetry/opentelemetry-dotnet" }
        });

        var spans = new List<Span>() { span };
        await entityFrameworkSpanWriter.WriteAsync(spans);
    }
}
