// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.ComponentModel;

namespace Mocha.Query.Prometheus.PromQL.Values;

public enum ResultValueType
{
    [Description("none")] None,
    [Description("vector")] Vector,
    [Description("scalar")] Scalar,
    [Description("matrix")] Matrix,
    [Description("string")] String
}
