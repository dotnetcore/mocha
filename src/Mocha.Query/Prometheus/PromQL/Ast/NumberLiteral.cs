// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Query.Prometheus.PromQL.Ast;

public class NumberLiteral : Expression
{
    public override PrometheusValueType Type => PrometheusValueType.Scalar;

    public double Value { get; init; }
}
