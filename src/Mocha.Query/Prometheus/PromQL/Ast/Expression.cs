// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Mocha.Query.Prometheus.PromQL.Ast;

#if DEBUG
[DebuggerDisplay("{ExpressionText}")]
#endif
public abstract class Expression : Node
{
    public abstract PrometheusValueType Type { get; }

#if DEBUG
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string? ExpressionText { get; init; }
#endif

}
