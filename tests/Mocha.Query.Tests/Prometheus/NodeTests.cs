// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Models.Metrics;
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
            Func = new Function
            {
                Name = FunctionName.HistogramQuantile,
                ArgTypes = [PrometheusValueType.Scalar, PrometheusValueType.Vector],
                ReturnType = PrometheusValueType.Vector,
                Call = Functions.FuncHistogramQuantile
            },
            Args =
            [
                new NumberLiteral { Value = 0.9 },
                new AggregateExpression
                {
                    Op = AggregationOp.Sum,
                    Expression = new Call
                    {
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
            new NumberLiteral { Value = 0.9 },
            new AggregateExpression
            {
                Op = AggregationOp.Sum,
                Expression = new Call
                {
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
