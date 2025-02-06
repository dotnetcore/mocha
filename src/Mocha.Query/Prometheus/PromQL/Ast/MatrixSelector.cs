// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Storage.Prometheus.Metrics;

namespace Mocha.Query.Prometheus.PromQL.Ast;

public class MatrixSelector : Expression
{
    public override PrometheusValueType Type => PrometheusValueType.Matrix;

    public required string? Name { get; init; }

    public TimeSpan Range { get; init; }

    public required IEnumerable<LabelMatcher> LabelMatchers { get; init; }

    /// <summary>
    /// The offset used during the query execution which is calculated using the original offset,
    /// at modifier time, eval time, and subquery offsets in the Ast tree.
    /// </summary>
    public TimeSpan Offset { get; set; }

    public IEnumerable<TimeSeries>? Series { get; set; }
}
