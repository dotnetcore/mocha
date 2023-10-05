using Grpc.Core;
using OpenTelemetry.Proto.Collector.Trace.V1;

namespace Mocha.Distributor.Receivers;

// ReSharper disable once IdentifierTypo
public class OtlpTraceExportService : TraceService.TraceServiceBase
{
    public override Task<ExportTraceServiceResponse> Export(ExportTraceServiceRequest request, ServerCallContext context)
    {
        return Task.FromResult(new ExportTraceServiceResponse());
    }
}