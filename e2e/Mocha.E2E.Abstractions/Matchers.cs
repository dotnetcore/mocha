// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.E2E.Abstractions;

/// <summary>
/// Assertion helpers layered over <see cref="QueryClient"/>. They translate a case's declarative
/// <c>expect</c> block (which signals to look for, and the Prometheus matcher vocabulary) into the
/// existing poll-until-visible waiters, so both the Tier-1 runner and any future consumer share one
/// definition of "the telemetry became readable through the Query API".
/// </summary>
public static class Matchers
{
    /// <summary>Prometheus matcher: the instant query returns a non-empty result vector.</summary>
    /// <remarks>
    /// This is the only Prometheus matcher today because it is exactly what <see cref="QueryClient"/>
    /// proves against the running stack (<c>WaitForMetricAsync</c>). Richer matchers (exact value,
    /// label presence, sample count) can be added here as the vocabulary grows.
    /// </remarks>
    public const string NonEmptyVector = "nonEmptyVector";

    /// <summary>
    /// Asserts the Jaeger trace surface for a run: optionally the service, each expected operation,
    /// and optionally a trace retrievable by id. Each check polls until visible or times out.
    /// </summary>
    public static async Task AssertJaegerAsync(
        QueryClient client,
        string serviceName,
        bool expectService,
        IReadOnlyList<string> expectedOperations,
        string? traceId,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (expectService)
        {
            await client.WaitForServiceAsync(serviceName, timeout, cancellationToken);
        }

        foreach (var operation in expectedOperations)
        {
            await client.WaitForOperationAsync(serviceName, operation, timeout, cancellationToken);
        }

        if (!string.IsNullOrEmpty(traceId))
        {
            await client.WaitForTraceByIdAsync(traceId, timeout, cancellationToken);
        }
    }

    /// <summary>
    /// Asserts a single Prometheus expectation: runs <paramref name="query"/> and verifies it
    /// satisfies <paramref name="expect"/>. Only <see cref="NonEmptyVector"/> is supported today.
    /// </summary>
    public static Task AssertPrometheusAsync(
        QueryClient client,
        string query,
        string expect,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(expect, NonEmptyVector, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                $"Prometheus matcher '{expect}' is not supported yet; only '{NonEmptyVector}' is implemented.");
        }

        return client.WaitForMetricAsync(query, timeout, cancellationToken);
    }
}
