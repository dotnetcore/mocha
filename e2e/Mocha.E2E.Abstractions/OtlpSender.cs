// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Grpc.Core;
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
    // The distributor may still be finishing its own startup (opening its MySQL / InfluxDB
    // connections) when the first case fires. During that window the gRPC channel reports
    // Unavailable (connection refused / not yet listening). Retry the initial export for a
    // short bounded window so a healthy-but-slow-starting distributor does not fail the run.
    // This is defense-in-depth on top of the docker-compose healthcheck gating.
    private static readonly TimeSpan _startupRetryWindow = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _startupRetryDelay = TimeSpan.FromSeconds(1);

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
        var response = await SendWithStartupRetryAsync(
            ct => _traceClient.ExportAsync(request, cancellationToken: ct).ResponseAsync,
            cancellationToken);
        var rejected = response.PartialSuccess?.RejectedSpans ?? 0;
        if (rejected != 0)
        {
            throw new InvalidOperationException(
                $"Distributor rejected {rejected} span(s): {response.PartialSuccess?.ErrorMessage}");
        }
    }

    public async Task SendMetricsAsync(ExportMetricsServiceRequest request, CancellationToken cancellationToken)
    {
        var response = await SendWithStartupRetryAsync(
            ct => _metricsClient.ExportAsync(request, cancellationToken: ct).ResponseAsync,
            cancellationToken);
        var rejected = response.PartialSuccess?.RejectedDataPoints ?? 0;
        if (rejected != 0)
        {
            throw new InvalidOperationException(
                $"Distributor rejected {rejected} data point(s): {response.PartialSuccess?.ErrorMessage}");
        }
    }

    /// <summary>
    /// Invokes a gRPC export and retries while the channel reports a transient startup condition
    /// (<see cref="StatusCode.Unavailable"/>, i.e. connection refused / not yet listening) for up
    /// to <see cref="_startupRetryWindow"/>. Any other failure, or exhausting the window, rethrows
    /// the last <see cref="RpcException"/>.
    /// </summary>
    private static async Task<TResponse> SendWithStartupRetryAsync<TResponse>(
        Func<CancellationToken, Task<TResponse>> sendAsync,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + _startupRetryWindow;
        while (true)
        {
            try
            {
                return await sendAsync(cancellationToken);
            }
            catch (RpcException ex) when (IsTransientStartup(ex.StatusCode) && DateTime.UtcNow < deadline)
            {
                await Task.Delay(_startupRetryDelay, cancellationToken);
            }
        }
    }

    private static bool IsTransientStartup(StatusCode statusCode) =>
        statusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded;

    public void Dispose()
    {
        _channel.Dispose();
    }
}
