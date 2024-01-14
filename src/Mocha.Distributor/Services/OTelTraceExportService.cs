// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Grpc.Core;
using Mocha.Core.Buffer;
using Mocha.Core.Models.Trace;
using OpenTelemetry.Proto.Collector.Trace.V1;

namespace Mocha.Distributor.Services;

public class OTelTraceExportService(IBufferQueue bufferQueue) : TraceService.TraceServiceBase
{
    private readonly IBufferProducer<MochaSpan> _bufferProducer =
        bufferQueue.CreateProducer<MochaSpan>("otlp-span");

    public override async Task<ExportTraceServiceResponse> Export(
        ExportTraceServiceRequest request,
        ServerCallContext context)
    {
        var spans = request.ResourceSpans
            .SelectMany(resourceSpans => resourceSpans.ScopeSpans
                .SelectMany(scopeSpans => scopeSpans.Spans
                    .Select(span => span.ToMochaSpan(resourceSpans.Resource)))).ToArray();

        var totalSpanCount = spans.Length;
        var acceptedSpanCount = 0;

        try
        {
            for (; acceptedSpanCount < totalSpanCount; acceptedSpanCount++)
            {
                await _bufferProducer.ProduceAsync(spans[acceptedSpanCount]);
            }
        }
        catch (Exception ex)
        {
            return new ExportTraceServiceResponse
            {
                PartialSuccess = new ExportTracePartialSuccess
                {
                    RejectedSpans = totalSpanCount - acceptedSpanCount,
                    ErrorMessage = ex.Message
                }
            };
        }

        return new ExportTraceServiceResponse();
    }
}
