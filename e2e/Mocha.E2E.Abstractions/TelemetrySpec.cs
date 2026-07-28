// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.E2E.Abstractions;

/// <summary>
/// Neutral, transport-agnostic description of the telemetry a single e2e case sends. It carries
/// only scalars, enum-as-string values and attribute maps; all of the OTLP proto construction and
/// the deterministic-identifier tricks live in <see cref="TelemetryFactory"/>.
/// </summary>
/// <remarks>
/// This type deliberately does NOT reference the Tier-1 YAML POCO (<c>CaseSpec</c>). Keeping the
/// builder input neutral means <see cref="TelemetryFactory"/> can be shared by the Tier-1 runner
/// and the Tier-2 xUnit component tests without either tier depending on the other.
/// </remarks>
public sealed class TelemetrySpec
{
    /// <summary>
    /// Logical service name before the per-run suffix is appended. <c>${runId}</c> tokens in this
    /// value and in any attribute value are substituted with the generated run id.
    /// </summary>
    public required string ServiceName { get; init; }

    /// <summary>Resource-level attributes (post token-substitution) attached to every signal.</summary>
    public IReadOnlyDictionary<string, string> ResourceAttributes { get; init; }
        = new Dictionary<string, string>();

    public IReadOnlyList<SpanSpec> Spans { get; init; } = [];

    public IReadOnlyList<MetricSpec> Metrics { get; init; } = [];
}

/// <summary>A single span to emit. <c>Kind</c> and <c>Status</c> are supplied as strings.</summary>
public sealed class SpanSpec
{
    /// <summary>Symbolic id used to correlate a span with an <c>expect</c> matcher (e.g. traceById).</summary>
    public required string Id { get; init; }

    public required string Name { get; init; }

    /// <summary>OTLP span kind as a string: unspecified/internal/server/client/producer/consumer.</summary>
    public string Kind { get; init; } = "server";

    /// <summary>OTLP status as a string: unset/ok/error.</summary>
    public string Status { get; init; } = "ok";

    public long DurationMs { get; init; } = 1;

    public IReadOnlyDictionary<string, string> Attributes { get; init; }
        = new Dictionary<string, string>();
}

/// <summary>A single metric data point to emit. Only the gauge type is exercised today.</summary>
public sealed class MetricSpec
{
    /// <summary>Logical metric name before the per-run suffix is appended.</summary>
    public required string Name { get; init; }

    /// <summary>Metric type as a string. Only <c>gauge</c> is supported today.</summary>
    public string Type { get; init; } = "gauge";

    /// <summary>OTLP unit. <c>"1"</c> maps to an empty stored unit (no name suffix).</summary>
    public string Unit { get; init; } = "1";

    public double Value { get; init; }

    public IReadOnlyDictionary<string, string> Attributes { get; init; }
        = new Dictionary<string, string>();
}
