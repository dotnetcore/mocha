// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.E2E.Abstractions;

namespace Mocha.EndToEnd;

/// <summary>
/// YAML-deserialized description of one Tier-1 case. It intentionally mirrors the on-disk schema
/// (scalars, enum-as-string, attribute maps, matcher choices) and delegates all proto construction
/// to <see cref="TelemetryFactory"/> via <see cref="ToTelemetrySpec"/>. Adding a case is therefore
/// "drop a YAML file, no recompile".
/// </summary>
public sealed class CaseSpec
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;

    public SendSpec Send { get; set; } = new();

    public ExpectSpec Expect { get; set; } = new();

    /// <summary>Projects the YAML case onto the neutral builder input consumed by the factory.</summary>
    public TelemetrySpec ToTelemetrySpec()
    {
        return new TelemetrySpec
        {
            ServiceName = Send.Resource.ServiceName,
            ResourceAttributes = Send.Resource.Attributes,
            Spans = Send.Spans.Select(s => new SpanSpec
            {
                Id = s.Id,
                Name = s.Name,
                Kind = s.Kind,
                Status = s.Status,
                DurationMs = s.DurationMs,
                Attributes = s.Attributes
            }).ToList(),
            Metrics = Send.Metrics.Select(m => new MetricSpec
            {
                Name = m.Name,
                Type = m.Type,
                Unit = m.Unit,
                Value = m.Value,
                Attributes = m.Attributes
            }).ToList()
        };
    }

    public sealed class SendSpec
    {
        public ResourceSpec Resource { get; set; } = new();

        public List<CaseSpanSpec> Spans { get; set; } = [];

        public List<CaseMetricSpec> Metrics { get; set; } = [];
    }

    public sealed class ResourceSpec
    {
        public string ServiceName { get; set; } = "mocha-e2e";

        public Dictionary<string, string> Attributes { get; set; } = [];
    }

    public sealed class CaseSpanSpec
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Kind { get; set; } = "server";

        public string Status { get; set; } = "ok";

        public long DurationMs { get; set; } = 1;

        public Dictionary<string, string> Attributes { get; set; } = [];
    }

    public sealed class CaseMetricSpec
    {
        public string Name { get; set; } = string.Empty;

        public string Type { get; set; } = "gauge";

        public string Unit { get; set; } = "1";

        public double Value { get; set; }

        public Dictionary<string, string> Attributes { get; set; } = [];
    }

    public sealed class ExpectSpec
    {
        public JaegerExpect? Jaeger { get; set; }

        public List<PrometheusExpect> Prometheus { get; set; } = [];
    }

    public sealed class JaegerExpect
    {
        public bool Service { get; set; }

        public List<string> Operations { get; set; } = [];

        /// <summary>Symbolic span id whose lowercase-hex trace id must be retrievable, if set.</summary>
        public string? TraceById { get; set; }
    }

    public sealed class PrometheusExpect
    {
        public string Query { get; set; } = string.Empty;

        public string Expect { get; set; } = Matchers.NonEmptyVector;
    }
}
