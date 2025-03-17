// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Query.Prometheus.PromQL.Ast;

/// <summary>
/// BinaryExpr represents a binary expression between two child expressions.
/// </summary>
public class BinaryExpression : Expression
{
    public override PrometheusValueType Type
    {
        get
        {
            if (LHS.Type == PrometheusValueType.Scalar && RHS.Type == PrometheusValueType.Scalar)
            {
                return PrometheusValueType.Scalar;
            }

            return PrometheusValueType.Vector;
        }
    }

    /// <summary>
    /// The operation of the expression.
    /// </summary>
    public Operator Op { get; init; }

    /// <summary>
    /// The operand on the left side of the operator.
    /// </summary>
    public required Expression LHS { get; init; }

    /// <summary>
    /// The operand on the right side of the operator.
    /// </summary>
    public required Expression RHS { get; init; }

    /// <summary>
    /// The matching behavior for the operation if both operands are Vectors.
    /// If they are not this field is null.
    /// </summary>
    public VectorMatching? VectorMatching { get; init; }

    /// <summary>
    /// If a comparison operator, return 0/1 rather than filtering.
    /// </summary>
    public bool ReturnBool { get; init; }
}
