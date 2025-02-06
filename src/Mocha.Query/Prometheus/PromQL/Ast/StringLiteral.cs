// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Query.Prometheus.PromQL.Ast;

public class StringLiteral : Expression
{
    public override PrometheusValueType Type => PrometheusValueType.String;

    public string? Value { get; init; }
}
