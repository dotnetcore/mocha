// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Net;
using System.Text.Json;

namespace Mocha.E2E.Abstractions;

/// <summary>
/// Thin HTTP client over the Query API (<c>:5775</c>) that polls the Jaeger and Prometheus
/// read endpoints until the asynchronously-drained telemetry becomes visible.
/// </summary>
/// <remarks>
/// Writes are eventually consistent: the distributor buffers OTLP data in memory and a hosted
/// <c>StorageExporter</c> drains it to storage in the background. Every assertion therefore polls
/// with a timeout instead of reading once.
/// </remarks>
public sealed class QueryClient : IDisposable
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public QueryClient(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(10) };
    }

    /// <summary>Waits until the Query API answers HTTP requests at all (process readiness).</summary>
    public async Task WaitUntilReadyAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        await PollAsync(
            "Query API readiness",
            timeout,
            async ct =>
            {
                try
                {
                    using var response = await _http.GetAsync("/jaeger/api/services", ct);
                    return response.StatusCode == HttpStatusCode.OK;
                }
                catch (Exception)
                {
                    return false;
                }
            },
            cancellationToken);
    }

    /// <summary>Polls Jaeger <c>/jaeger/api/services</c> until the run's service name appears.</summary>
    public Task WaitForServiceAsync(string serviceName, TimeSpan timeout, CancellationToken cancellationToken)
    {
        return PollAsync(
            $"Jaeger service '{serviceName}' to appear",
            timeout,
            async ct =>
            {
                var services = await GetJsonDataAsync<List<string>>("/jaeger/api/services", ct);
                return services is not null && services.Contains(serviceName);
            },
            cancellationToken);
    }

    /// <summary>
    /// Polls Jaeger <c>/jaeger/api/services/{svc}/operations</c> until the run's operation appears.
    /// </summary>
    public Task WaitForOperationAsync(
        string serviceName,
        string operationName,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        return PollAsync(
            $"Jaeger operation '{operationName}' for service '{serviceName}'",
            timeout,
            async ct =>
            {
                var operations = await GetJsonDataAsync<List<string>>(
                    $"/jaeger/api/services/{Uri.EscapeDataString(serviceName)}/operations", ct);
                return operations is not null && operations.Contains(operationName);
            },
            cancellationToken);
    }

    /// <summary>Polls Jaeger <c>/jaeger/api/traces/{id}</c> until the trace is retrievable by id.</summary>
    public Task WaitForTraceByIdAsync(string traceId, TimeSpan timeout, CancellationToken cancellationToken)
    {
        return PollAsync(
            $"Jaeger trace '{traceId}' to be retrievable",
            timeout,
            async ct =>
            {
                using var doc = await GetJsonDocumentAsync(
                    $"/jaeger/api/traces/{Uri.EscapeDataString(traceId)}", ct);
                if (doc is null)
                {
                    return false;
                }

                if (!doc.RootElement.TryGetProperty("data", out var data)
                    || data.ValueKind != JsonValueKind.Array)
                {
                    return false;
                }

                foreach (var trace in data.EnumerateArray())
                {
                    if (trace.TryGetProperty("traceID", out var id)
                        && string.Equals(id.GetString(), traceId, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            },
            cancellationToken);
    }

    /// <summary>
    /// Polls the Prometheus instant-query endpoint (<c>POST /prometheus/api/v1/query</c>,
    /// form-urlencoded) until the gauge is returned as a non-empty vector.
    /// </summary>
    public Task WaitForMetricAsync(string metricName, TimeSpan timeout, CancellationToken cancellationToken)
    {
        return PollAsync(
            $"Prometheus metric '{metricName}' to be queryable",
            timeout,
            async ct =>
            {
                using var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["query"] = metricName
                });

                using var response = await _http.PostAsync("/prometheus/api/v1/query", content, ct);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return false;
                }

                var body = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (!root.TryGetProperty("status", out var status)
                    || status.GetString() != "success")
                {
                    return false;
                }

                if (!root.TryGetProperty("data", out var data)
                    || !data.TryGetProperty("result", out var result)
                    || result.ValueKind != JsonValueKind.Array)
                {
                    return false;
                }

                return result.GetArrayLength() > 0;
            },
            cancellationToken);
    }

    private async Task<T?> GetJsonDataAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(path, cancellationToken);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return default;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("data", out var data))
        {
            return default;
        }

        return data.Deserialize<T>(_jsonOptions);
    }

    private async Task<JsonDocument?> GetJsonDocumentAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(path, cancellationToken);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonDocument.Parse(body);
    }

    private static async Task PollAsync(
        string description,
        TimeSpan timeout,
        Func<CancellationToken, Task<bool>> probe,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        var pollInterval = TimeSpan.FromMilliseconds(500);
        var attempts = 0;
        Exception? lastError = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            attempts++;

            try
            {
                if (await probe(cancellationToken))
                {
                    Console.WriteLine($"  [ok] {description} (after {attempts} attempt(s))");
                    return;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastError = ex;
            }

            await Task.Delay(pollInterval, cancellationToken);
        }

        var suffix = lastError is null ? string.Empty : $" Last error: {lastError.Message}";
        throw new TimeoutException(
            $"Timed out after {timeout.TotalSeconds:0}s waiting for {description} ({attempts} attempt(s)).{suffix}");
    }

    public void Dispose()
    {
        _http.Dispose();
    }
}
