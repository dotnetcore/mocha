// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Core.Models.Trace;
using Mocha.Core.Storage;
using Mocha.Core.Storage.Jaeger;
using Mocha.E2E.Abstractions;
using Mocha.E2E.Component.Tests.Fixtures;
using Mocha.Storage;
using Mocha.Storage.EntityFrameworkCore.Trace;
using OpenTelemetry.Proto.Trace.V1;
using Xunit;

namespace Mocha.E2E.Component.Tests;

/// <summary>
/// Tier-2 component test: exercises the EFCore tracing storage adapter against a REAL MySQL 8.2.0
/// container (via Testcontainers), not the in-memory provider. It builds an OTLP span with the
/// shared <see cref="TelemetryFactory"/>, runs it through the production OTLP-&gt;Mocha conversion,
/// writes it with <see cref="ITelemetryDataWriter{T}"/> (<c>EFSpanWriter</c>), and reads it back
/// through <see cref="IJaegerSpanReader"/> (<c>EFJaegerSpanReader</c>) — proving the adapter round
/// trips against the same schema and engine the release images use.
/// </summary>
[Collection(MySqlCollection.Name)]
public sealed class MySqlSpanStorageRoundTripTests(MySqlContainerFixture mySql)
{
    [Fact]
    public async Task WriteThenReadByTraceId_RoundTripsThroughRealMySql()
    {
        // Arrange: wire the real EFCore/MySQL storage the same way Mocha.Query.Program does, but
        // pointed at the container connection string (mirrors EFSpanWriterTests, real engine).
        var services = new ServiceCollection();
        services.AddStorage()
            .WithTracing(tracing =>
            {
                tracing.UseEntityFrameworkCore(options =>
                {
                    options.UseMySql(mySql.ConnectionString, ServerVersion.AutoDetect(mySql.ConnectionString));
                });
            });

        await using var provider = services.BuildServiceProvider();
        var writer = provider.GetRequiredService<ITelemetryDataWriter<MochaSpan>>();
        var reader = provider.GetRequiredService<IJaegerSpanReader>();

        // Build a deterministic OTLP payload with the shared factory, then convert to the storage
        // model using the same production extension the distributor uses. No hand-built spans.
        var factory = new TelemetryFactory(new TelemetrySpec
        {
            ServiceName = "component-mysql",
            Spans =
            [
                new SpanSpec
                {
                    Id = "root",
                    Name = "component-root-span",
                    Kind = "server",
                    Status = "ok",
                    DurationMs = 3,
                    Attributes = new Dictionary<string, string> { ["http.method"] = "GET" }
                }
            ]
        });

        var traceRequest = factory.BuildTraceRequest();
        var mochaSpans = ToMochaSpans(traceRequest).ToList();
        mochaSpans.Should().ContainSingle();
        var expectedTraceId = factory.TraceIdFor("root");

        // Act
        await writer.WriteAsync(mochaSpans);
        var traces = (await reader.FindTracesAsync([expectedTraceId])).ToList();

        // Assert: the trace comes back with the right id, service and operation, from real MySQL.
        traces.Should().ContainSingle();
        var trace = traces[0];
        trace.TraceID.Should().Be(expectedTraceId);
        trace.Spans.Should().ContainSingle();
        trace.Spans[0].OperationName.Should().Be("component-root-span");
        trace.Processes.Values.Should().Contain(p => p.ServiceName == factory.ServiceName);

        // And a service/operation-scoped query resolves the same span.
        var byService = (await reader.FindTracesAsync(new Core.Storage.Jaeger.Trace.JaegerTraceQueryParameters
        {
            ServiceName = factory.ServiceName,
            OperationName = "component-root-span"
        })).ToList();
        byService.Should().Contain(t => t.TraceID == expectedTraceId);
    }

    private static IEnumerable<MochaSpan> ToMochaSpans(
        OpenTelemetry.Proto.Collector.Trace.V1.ExportTraceServiceRequest request)
    {
        foreach (var resourceSpans in request.ResourceSpans)
        {
            var resource = resourceSpans.Resource;
            foreach (var scopeSpans in resourceSpans.ScopeSpans)
            {
                foreach (var span in scopeSpans.Spans)
                {
                    yield return span.ToMochaSpan(resource);
                }
            }
        }
    }
}
