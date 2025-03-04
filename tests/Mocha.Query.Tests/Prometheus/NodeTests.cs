// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Storage.Prometheus;
using Mocha.Core.Storage.Prometheus.Metrics;
using Mocha.Query.Prometheus.PromQL.Ast;
using Mocha.Query.Prometheus.PromQL.Engine;

namespace Mocha.Query.Tests.Prometheus;

public class NodeTests
{
    [Fact]
    public void Inspect()
    {
        var expression = new Call
        {
            ExpressionText = "histogram_quantile(0.9, sum(rate(foo[5m])) by (le))",
            Func = new Function
            {
                Name = FunctionName.HistogramQuantile,
                ArgTypes = [PrometheusValueType.Scalar, PrometheusValueType.Vector],
                ReturnType = PrometheusValueType.Vector,
                Call = Functions.FuncHistogramQuantile
            },
            Args =
            [
                new NumberLiteral { ExpressionText = "0.9", Value = 0.9 },
                new AggregateExpression
                {
                    ExpressionText = "sum(rate(foo[5m]))",
                    Op = AggregationOp.Sum,
                    Expression = new Call
                    {
                        ExpressionText = "rate(foo[5m])",
                        Func = new Function
                        {
                            Name = FunctionName.Rate,
                            ArgTypes = [PrometheusValueType.Vector],
                            ReturnType = PrometheusValueType.Vector,
                            Call = Functions.FuncRate
                        },
                        Args =
                        [
                            new MatrixSelector
                            {
                                ExpressionText = "foo[5m]",
                                Name = "foo",
                                Range = TimeSpan.FromMinutes(5),
                                LabelMatchers =
                                [
                                    new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                                ],
                                Series = []
                            }
                        ],
                    },
                    Grouping = ["le"]
                }
            ]
        };

        var result = expression.Inspect().ToList();

        // depth-first traversal
        var expected = new List<Expression>
        {
            expression,
            new NumberLiteral { ExpressionText = "0.9", Value = 0.9 },
            new AggregateExpression
            {
                ExpressionText = "sum(rate(foo[5m]))",
                Op = AggregationOp.Sum,
                Expression = new Call
                {
                    ExpressionText = "rate(foo[5m])",
                    Func = new Function
                    {
                        Name = FunctionName.Rate,
                        ArgTypes = [PrometheusValueType.Vector],
                        ReturnType = PrometheusValueType.Vector,
                        Call = Functions.FuncRate
                    },
                    Args =
                    [
                        new MatrixSelector
                        {
                            ExpressionText = "foo[5m]",
                            Name = "foo",
                            Range = TimeSpan.FromMinutes(5),
                            LabelMatchers =
                            [
                                new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                            ],
                            Series = []
                        }
                    ],
                },
                Grouping = ["le"]
            },
            new Call
            {
                ExpressionText = "rate(foo[5m])",
                Func = new Function
                {
                    Name = FunctionName.Rate,
                    ArgTypes = [PrometheusValueType.Vector],
                    ReturnType = PrometheusValueType.Vector,
                    Call = Functions.FuncRate
                },
                Args =
                [
                    new MatrixSelector
                    {
                        ExpressionText = "foo[5m]",
                        Name = "foo",
                        Range = TimeSpan.FromMinutes(5),
                        LabelMatchers =
                        [
                            new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                        ],
                        Series = []
                    }
                ],
            },
            new MatrixSelector
            {
                ExpressionText = "foo[5m]",
                Name = "foo",
                Range = TimeSpan.FromMinutes(5),
                LabelMatchers =
                [
                    new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                ],
                Series = []
            }
        };

        result.Should().BeEquivalentTo(expected, options => options.RespectingRuntimeTypes());
    }
}
