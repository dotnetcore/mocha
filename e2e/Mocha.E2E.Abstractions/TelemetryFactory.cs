// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Google.Protobuf;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Metrics.V1;
using OpenTelemetry.Proto.Resource.V1;
using OpenTelemetry.Proto.Trace.V1;

namespace Mocha.E2E.Abstractions;

/// <summary>
/// Turns a neutral <see cref="TelemetrySpec"/> into a realistic OTLP trace and metric payload for a
/// single end-to-end run, and exposes the identifiers the Query API is expected to surface after
/// ingestion.
/// </summary>
/// <remarks>
/// This is the sole owner of the determinism tricks that keep assertions immune to leftover data
/// from previous runs (the same tricks the original console-only <c>E2ETelemetry</c> used):
/// <list type="bullet">
///   <item>A unique run suffix is appended to the service name and every metric name, and every
///   <c>${runId}</c> token in attribute values is substituted with the run id.</item>
///   <item>Each span's trace id is 16 random bytes; the distributor converts it to a lowercase hex
///   string. Forcing the first eight bytes to be non-zero keeps all 32 hex characters, so the hex
///   equals <c>Convert.ToHexString(bytes).ToLowerInvariant()</c>, which is what we assert against.</item>
///   <item>A gauge unit of <c>"1"</c> is mapped by the distributor to an empty unit, so the stored
///   Prometheus metric name has no unit suffix and equals the name we send.</item>
/// </list>
/// </remarks>
public sealed class TelemetryFactory
{
    private const string RunIdToken = "${runId}";

    private readonly TelemetrySpec _spec;
    private readonly List<ResolvedSpan> _spans;
    private readonly List<ResolvedMetric> _metrics;
    private readonly Dictionary<string, string> _resourceAttributes;

    /// <summary>
    /// Builds a factory for <paramref name="spec"/>. A run id is generated when
    /// <paramref name="runId"/> is null, matching the original 12-hex-character format.
    /// </summary>
    public TelemetryFactory(TelemetrySpec spec, string? runId = null)
    {
        ArgumentNullException.ThrowIfNull(spec);

        _spec = spec;
        RunId = runId ?? Guid.NewGuid().ToString("N")[..12];
        ServiceName = $"{spec.ServiceName}-{RunId}";

        _resourceAttributes = new Dictionary<string, string>
        {
            ["service.name"] = ServiceName,
            ["service.instance.id"] = RunId
        };
        foreach (var (key, value) in spec.ResourceAttributes)
        {
            _resourceAttributes[key] = Substitute(value);
        }

        _spans = spec.Spans.Select(ResolveSpan).ToList();
        _metrics = spec.Metrics.Select(ResolveMetric).ToList();
    }

    /// <summary>The per-run id (12 lowercase hex characters).</summary>
    public string RunId { get; }

    /// <summary>Service name as the Query API stores and returns it (<c>&lt;name&gt;-&lt;runId&gt;</c>).</summary>
    public string ServiceName { get; }

    /// <summary>Resolved metric names (suffixed with the run id) in declaration order.</summary>
    public IReadOnlyList<string> MetricNames => _metrics.Select(m => m.Name).ToList();

    /// <summary>Resolved operation (span) names in declaration order.</summary>
    public IReadOnlyList<string> OperationNames => _spans.Select(s => s.Name).ToList();

    /// <summary>Looks up the lowercase-hex trace id for a symbolic span id from the spec.</summary>
    public string TraceIdFor(string symbolicSpanId)
    {
        var span = _spans.FirstOrDefault(s => s.SymbolicId == symbolicSpanId)
                   ?? throw new InvalidOperationException(
                       $"No span with symbolic id '{symbolicSpanId}' in the telemetry spec.");
        return span.TraceId;
    }

    /// <summary>The lowercase-hex trace id of the first span, if any.</summary>
    public string? PrimaryTraceId => _spans.Count > 0 ? _spans[0].TraceId : null;

    public ExportTraceServiceRequest BuildTraceRequest()
    {
        var resourceSpans = new ResourceSpans { Resource = BuildResource() };
        var scopeSpans = new ScopeSpans
        {
            Scope = new InstrumentationScope { Name = "Mocha.E2E", Version = "1.0.0" }
        };

        foreach (var span in _spans)
        {
            var protoSpan = new Span
            {
                TraceId = ByteString.CopyFrom(span.TraceIdBytes),
                SpanId = ByteString.CopyFrom(span.SpanIdBytes),
                Name = span.Name,
                Kind = span.Kind,
                StartTimeUnixNano = span.StartTimeUnixNano,
                EndTimeUnixNano = span.EndTimeUnixNano,
                Status = new Status { Code = span.StatusCode }
            };
            foreach (var (key, value) in span.Attributes)
            {
                protoSpan.Attributes.Add(StringAttribute(key, value));
            }

            scopeSpans.Spans.Add(protoSpan);
        }

        resourceSpans.ScopeSpans.Add(scopeSpans);
        return new ExportTraceServiceRequest { ResourceSpans = { resourceSpans } };
    }

    public ExportMetricsServiceRequest BuildMetricsRequest()
    {
        var resourceMetrics = new ResourceMetrics { Resource = BuildResource() };
        var scopeMetrics = new ScopeMetrics
        {
            Scope = new InstrumentationScope { Name = "Mocha.E2E", Version = "1.0.0" }
        };

        foreach (var metric in _metrics)
        {
            var dataPoint = new NumberDataPoint
            {
                TimeUnixNano = metric.TimestampUnixNano,
                StartTimeUnixNano = metric.TimestampUnixNano,
                AsDouble = metric.Value
            };
            foreach (var (key, value) in metric.Attributes)
            {
                dataPoint.Attributes.Add(StringAttribute(key, value));
            }

            var protoMetric = new Metric
            {
                Name = metric.Name,
                Description = "Mocha end-to-end metric",
                Unit = metric.Unit,
                Gauge = new Gauge { DataPoints = { dataPoint } }
            };
            scopeMetrics.Metrics.Add(protoMetric);
        }

        resourceMetrics.ScopeMetrics.Add(scopeMetrics);
        return new ExportMetricsServiceRequest { ResourceMetrics = { resourceMetrics } };
    }

    private Resource BuildResource()
    {
        var resource = new Resource();
        foreach (var (key, value) in _resourceAttributes)
        {
            resource.Attributes.Add(StringAttribute(key, value));
        }

        return resource;
    }

    private ResolvedSpan ResolveSpan(SpanSpec spec)
    {
        var traceIdBytes = new byte[16];
        var spanIdBytes = new byte[8];
        Random.Shared.NextBytes(traceIdBytes);
        Random.Shared.NextBytes(spanIdBytes);

        // Guarantee the first eight trace-id bytes are non-zero so the server-side hex conversion
        // keeps all 32 characters (see the class remarks). Same rationale for the span id.
        traceIdBytes[0] = 0x2a;
        spanIdBytes[0] = 0x2a;

        var nowUnixNano = CurrentUnixNano();
        var attributes = spec.Attributes.ToDictionary(kvp => kvp.Key, kvp => Substitute(kvp.Value));

        return new ResolvedSpan
        {
            SymbolicId = spec.Id,
            Name = spec.Name,
            Kind = ParseSpanKind(spec.Kind),
            StatusCode = ParseStatusCode(spec.Status),
            StartTimeUnixNano = nowUnixNano,
            EndTimeUnixNano = nowUnixNano + (ulong)Math.Max(0, spec.DurationMs) * 1_000_000UL,
            TraceIdBytes = traceIdBytes,
            SpanIdBytes = spanIdBytes,
            TraceId = Convert.ToHexString(traceIdBytes).ToLowerInvariant(),
            Attributes = attributes
        };
    }

    private ResolvedMetric ResolveMetric(MetricSpec spec)
    {
        if (!string.Equals(spec.Type, "gauge", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                $"Metric type '{spec.Type}' is not supported yet; only 'gauge' is implemented.");
        }

        // Prometheus metric names must match [a-zA-Z_][a-zA-Z0-9_]*, so the run id (hex) is safe.
        var attributes = spec.Attributes.ToDictionary(kvp => kvp.Key, kvp => Substitute(kvp.Value));

        return new ResolvedMetric
        {
            Name = $"{spec.Name}_{RunId}",
            Unit = spec.Unit,
            Value = spec.Value,
            TimestampUnixNano = CurrentUnixNano(),
            Attributes = attributes
        };
    }

    private string Substitute(string value) => value.Replace(RunIdToken, RunId, StringComparison.Ordinal);

    private static Span.Types.SpanKind ParseSpanKind(string kind) => kind.ToLowerInvariant() switch
    {
        "unspecified" => Span.Types.SpanKind.Unspecified,
        "internal" => Span.Types.SpanKind.Internal,
        "server" => Span.Types.SpanKind.Server,
        "client" => Span.Types.SpanKind.Client,
        "producer" => Span.Types.SpanKind.Producer,
        "consumer" => Span.Types.SpanKind.Consumer,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown span kind.")
    };

    private static Status.Types.StatusCode ParseStatusCode(string status) => status.ToLowerInvariant() switch
    {
        "unset" => Status.Types.StatusCode.Unset,
        "ok" => Status.Types.StatusCode.Ok,
        "error" => Status.Types.StatusCode.Error,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown span status.")
    };

    private static KeyValue StringAttribute(string key, string value) =>
        new() { Key = key, Value = new AnyValue { StringValue = value } };

    private static ulong CurrentUnixNano() =>
        (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000UL;

    private sealed class ResolvedSpan
    {
        public required string SymbolicId { get; init; }

        public required string Name { get; init; }

        public required Span.Types.SpanKind Kind { get; init; }

        public required Status.Types.StatusCode StatusCode { get; init; }

        public required ulong StartTimeUnixNano { get; init; }

        public required ulong EndTimeUnixNano { get; init; }

        public required byte[] TraceIdBytes { get; init; }

        public required byte[] SpanIdBytes { get; init; }

        public required string TraceId { get; init; }

        public required Dictionary<string, string> Attributes { get; init; }
    }

    private sealed class ResolvedMetric
    {
        public required string Name { get; init; }

        public required string Unit { get; init; }

        public required double Value { get; init; }

        public required ulong TimestampUnixNano { get; init; }

        public required Dictionary<string, string> Attributes { get; init; }
    }
}
