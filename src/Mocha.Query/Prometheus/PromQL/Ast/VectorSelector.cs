// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Storage.Prometheus.Metrics;

namespace Mocha.Query.Prometheus.PromQL.Ast;

public class VectorSelector : Expression
{
    public override PrometheusValueType Type => PrometheusValueType.Vector;

    public string? Name { get; init; }

    public TimeSpan Offset { get; set; }

    public required IEnumerable<LabelMatcher> LabelMatchers { get; init; }

    public required IEnumerable<TimeSeries> Series { get; set; }
}
