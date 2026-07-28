// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Grpc.Net.Client;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;

namespace Mocha.E2E.Abstractions;

/// <summary>
/// Sends OTLP trace and metric export requests to the distributor over an insecure HTTP/2
/// gRPC channel (the distributor listens on plaintext h2c at <c>:4317</c>).
/// </summary>
public sealed class OtlpSender : IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly TraceService.TraceServiceClient _traceClient;
    private readonly MetricsService.MetricsServiceClient _metricsClient;

    public OtlpSender(string endpoint)
    {
        // The distributor uses HTTP/2 without TLS. Allow the unencrypted transport explicitly.
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        _channel = GrpcChannel.ForAddress(endpoint, new GrpcChannelOptions
        {
            HttpHandler = new SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true,
                PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan
            }
        });

        _traceClient = new TraceService.TraceServiceClient(_channel);
        _metricsClient = new MetricsService.MetricsServiceClient(_channel);
    }

    public async Task SendTraceAsync(ExportTraceServiceRequest request, CancellationToken cancellationToken)
    {
        var response = await _traceClient.ExportAsync(request, cancellationToken: cancellationToken);
        var rejected = response.PartialSuccess?.RejectedSpans ?? 0;
        if (rejected != 0)
        {
            throw new InvalidOperationException(
                $"Distributor rejected {rejected} span(s): {response.PartialSuccess?.ErrorMessage}");
        }
    }

    public async Task SendMetricsAsync(ExportMetricsServiceRequest request, CancellationToken cancellationToken)
    {
        var response = await _metricsClient.ExportAsync(request, cancellationToken: cancellationToken);
        var rejected = response.PartialSuccess?.RejectedDataPoints ?? 0;
        if (rejected != 0)
        {
            throw new InvalidOperationException(
                $"Distributor rejected {rejected} data point(s): {response.PartialSuccess?.ErrorMessage}");
        }
    }

    public void Dispose()
    {
        _channel.Dispose();
    }
}
