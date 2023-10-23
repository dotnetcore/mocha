// Licensed to the.NET Core Community under one or more agreements.
// The.NET Core Community licenses this file to you under the MIT license.

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
