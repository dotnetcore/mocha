// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Antlr4.Runtime;
using Mocha.Core.Storage.Prometheus.Metrics;
using Mocha.Query.Prometheus.PromQL.Ast;

namespace Mocha.Query.Prometheus.PromQL.Engine;

internal class MochaPromQLParserParser : IPromQLParser
{
    public Expression ParseExpression(string query) => Parse(query);

    public IEnumerable<LabelMatcher> ParseMetricSelector(string query)
    {
        var expression = Parse(query);
        if (expression is not VectorSelector vectorSelector)
        {
            throw new InvalidOperationException("Expected a vector selector");
        }

        return vectorSelector.LabelMatchers;
    }

    private static Expression Parse(string query)
    {
        var lexer = new PromQLLexer(CharStreams.fromString(query));
        var parser = new PromQLParser(new CommonTokenStream(lexer));
        var expression = new AstBuilder().Visit(parser.vectorOperation());
        return expression;
    }
}
