// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Query.Prometheus.PromQL.Ast;

public enum AggregationOp
{
    Sum,
    Min,
    Max,
    Avg,
    Group,
    StdDev,
    StdVar,
    Count,
    CountValues,
    BottomK,
    TopK,
    Quantile,
}
