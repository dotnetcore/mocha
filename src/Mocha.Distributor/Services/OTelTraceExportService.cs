using Grpc.Core;
using OpenTelemetry.Proto.Collector.Trace.V1;

namespace Mocha.Distributor.Services;

public class OTelTraceExportService : TraceService.TraceServiceBase
{
    public override Task<ExportTraceServiceResponse> Export(ExportTraceServiceRequest request, ServerCallContext context)
    {
        return Task.FromResult(new ExportTraceServiceResponse());
    }
}
