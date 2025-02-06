// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Query.Prometheus.PromQL.Ast;

public abstract class Node
{
    public IEnumerable<Node> Inspect()
    {
        yield return this;
        foreach (var child in Children())
        {
            foreach (var node in child.Inspect())
            {
                yield return node;
            }
        }
    }

    private IEnumerable<Node> Children()
    {
        IEnumerable<Node> children = this switch
        {
            EvalStatement n => [n.Expression],

            AggregateExpression n =>
                n switch
                {
                    { Expression: null, Parameter: null } => [],
                    { Expression: null } => [n.Parameter!],
                    { Parameter: null } => [n.Expression!],
                    _ => [n.Expression!, n.Parameter!]
                },

            BinaryExpression n => [n.LHS, n.RHS],

            Call n => n.Args,
            NumberLiteral => [],
            StringLiteral => [],
            VectorSelector => [],
            MatrixSelector => [],
            _ => throw new Exception($"Unhandled node type: {this.GetType().Name}")
        };
        return children;
    }
}
