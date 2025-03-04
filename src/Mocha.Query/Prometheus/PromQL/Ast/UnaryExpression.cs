// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Query.Prometheus.PromQL.Ast;

public class UnaryExpression : Expression
{
    public override PrometheusValueType Type => Expression.Type;

    public required Operator Operator { get; init; }

    public required Expression Expression { get; init; }
}
