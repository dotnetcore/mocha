// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Grpc.Core;
using Mocha.Core.Buffer;
using Mocha.Core.Models.Metadata;
using Mocha.Core.Models.Trace;
using OpenTelemetry.Proto.Collector.Trace.V1;

namespace Mocha.Distributor.Services;

public class OTelTraceExportService(IBufferQueue bufferQueue) : TraceService.TraceServiceBase
{
    private readonly IBufferProducer<MochaSpan> _bufferProducer =
        bufferQueue.CreateProducer<MochaSpan>("otlp-span");

    private readonly IBufferProducer<MochaSpanMetadata> _metadataBufferProducer =
        bufferQueue.CreateProducer<MochaSpanMetadata>("otlp-span-metadata");

    public override async Task<ExportTraceServiceResponse> Export(
        ExportTraceServiceRequest request,
        ServerCallContext context)
    {
        var spans = request.ResourceSpans
            .SelectMany(resourceSpans => resourceSpans.ScopeSpans
                .SelectMany(scopeSpans => scopeSpans.Spans
                    .Select(span => span.ToMochaSpan(resourceSpans.Resource)))).ToList();

        var spansMetadata = spans.Select(span => new MochaSpanMetadata
        {
            ServiceName = span.Resource.ServiceName,
            OperationName = span.SpanName
        }).DistinctBy(m => (m.ServiceName, m.OperationName)).ToList();

        foreach (var metadata in spansMetadata)
        {
            var valueTask = _metadataBufferProducer.ProduceAsync(metadata);
            if (!valueTask.IsCompletedSuccessfully)
            {
                await valueTask.AsTask();
            }
        }

        var totalSpanCount = spans.Count;
        var acceptedSpanCount = 0;

        try
        {
            for (; acceptedSpanCount < totalSpanCount; acceptedSpanCount++)
            {
                var valueTask = _bufferProducer.ProduceAsync(spans[acceptedSpanCount]);
                if (!valueTask.IsCompletedSuccessfully)
                {
                    await valueTask.AsTask();
                }
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
