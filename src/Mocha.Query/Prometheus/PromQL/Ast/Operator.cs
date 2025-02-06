// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Query.Prometheus.PromQL.Ast;

public enum Operator
{
    // Binary operators
    Add = 3,
    Sub = 4,
    Mul = 5,
    Div = 6,
    Mod = 7,
    Pow = 8,

    And = 9,
    Or = 10,
    Unless = 11,

    // Comparison operators
    Eql = 13,
    Neq = 14,
    Gtr = 15,
    Lss = 16,
    Gte = 17,
    Lte = 18
}
