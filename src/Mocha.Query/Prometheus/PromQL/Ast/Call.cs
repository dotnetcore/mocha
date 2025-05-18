// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Query.Prometheus.PromQL.Ast;

public class Call : Expression
{
    public override PrometheusValueType Type => Func.ReturnType;

    public required Function Func { get; init; }

    public required Expression[] Args { get; init; }
}
