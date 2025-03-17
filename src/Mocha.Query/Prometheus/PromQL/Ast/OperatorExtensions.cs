// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Query.Prometheus.PromQL.Ast;

public static class OperatorExtensions
{
    public static bool IsSetOperator(this Operator op)
    {
        return op switch
        {
            Operator.And => true,
            Operator.Or => true,
            Operator.Unless => true,
            _ => false,
        };
    }

    public static bool IsComparisonOperator(this Operator op)
    {
        return op switch
        {
            Operator.Eql => true,
            Operator.Neq => true,
            Operator.Gtr => true,
            Operator.Lss => true,
            Operator.Gte => true,
            Operator.Lte => true,
            _ => false,
        };
    }
}
