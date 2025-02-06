// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Query.Prometheus.PromQL.Ast;

/// <summary>
/// EvalStatement holds an expression and information
/// on the ranges it should be evaluated on.
/// </summary>
public class EvalStatement : Node
{
    public required Expression Expression { get; init; }

    public long StartTimestampUnixSec { get; init; }

    public long EndTimestampUnixSec { get; init; }

    public TimeSpan Interval { get; init; }
}
