// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.ComponentModel;

namespace Mocha.Query.Prometheus.PromQL.Ast;

public enum PrometheusValueType
{
    None,
    [Description("instant vector")]
    Vector,
    [Description("scalar")]
    Scalar,
    [Description("range vector")]
    Matrix,
    [Description("string")]
    String
}
