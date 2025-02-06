// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Query.Prometheus.PromQL.Ast;

public class AggregateExpression : Expression
{
    public override PrometheusValueType Type => PrometheusValueType.Vector;

    public AggregationOp Op { get; init; }

    public required Expression Expression { get; init; }

    public Expression? Parameter { get; init; }

    public required HashSet<string>? Grouping { get; init; }

    public bool Without { get; init; }
}
