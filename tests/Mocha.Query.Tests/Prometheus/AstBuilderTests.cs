// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Antlr4.Runtime;
using Mocha.Core.Extensions;
using Mocha.Core.Models.Metrics;
using Mocha.Core.Storage.Prometheus;
using Mocha.Core.Storage.Prometheus.Metrics;
using Mocha.Query.Prometheus.PromQL.Ast;
using Mocha.Query.Prometheus.PromQL.Engine;
using Xunit.Abstractions;

namespace Mocha.Query.Tests.Prometheus;

public class AstBuilderTests
{
    [Theory]
    [MemberData(nameof(TestCases))]
    public void Parse(TestCase testCase)
    {
        var input = testCase.Input;
        var expected = testCase.Expected;

        var lexer = new PromQLLexer(CharStreams.fromString(input));
        var parser = new PromQLParser(new CommonTokenStream(lexer));
        var actual = new AstBuilder().Visit(parser.vectorOperation());

        actual.Should().BeEquivalentTo(expected,
            options => options
                .RespectingRuntimeTypes()
                .Excluding(info => info.Path.EndsWith("ExpressionText")));
    }

    public static IEnumerable<object[]> TestCases => new TestCase[]
    {
        #region Scalars and scalar-to-scalar operations

        new() { Input = "1", Expected = new NumberLiteral { Value = 1d } },
        new()
        {
            Input = "1 + 1",
            Expected =
                new BinaryExpression
                {
                    Op = Operator.Add,
                    LHS = new NumberLiteral { Value = 1d },
                    RHS = new NumberLiteral { Value = 1d },
                    ReturnBool = false
                }
        },
        new()
        {
            Input = "1 - 1",
            Expected =
                new BinaryExpression
                {
                    Op = Operator.Sub,
                    LHS = new NumberLiteral { Value = 1d },
                    RHS = new NumberLiteral { Value = 1d },
                    ReturnBool = false
                }
        },
        new()
        {
            Input = "1 * 1",
            Expected =
                new BinaryExpression
                {
                    Op = Operator.Mul,
                    LHS = new NumberLiteral { Value = 1d },
                    RHS = new NumberLiteral { Value = 1d },
                    ReturnBool = false
                }
        },
        new()
        {
            Input = "1 % 1",
            Expected =
                new BinaryExpression
                {
                    Op = Operator.Mod,
                    LHS = new NumberLiteral { Value = 1d },
                    RHS = new NumberLiteral { Value = 1d },
                    ReturnBool = false
                }
        },
        new()
        {
            Input = "1 / 1",
            Expected =
                new BinaryExpression
                {
                    Op = Operator.Div,
                    LHS = new NumberLiteral { Value = 1d },
                    RHS = new NumberLiteral { Value = 1d },
                    ReturnBool = false
                }
        },
        new()
        {
            Input = "1 == bool 1",
            Expected =
                new BinaryExpression
                {
                    Op = Operator.Eql,
                    LHS = new NumberLiteral { Value = 1d },
                    RHS = new NumberLiteral { Value = 1d },
                    ReturnBool = true
                }
        },
        new()
        {
            Input = "1 != bool 1",
            Expected =
                new BinaryExpression
                {
                    Op = Operator.Neq,
                    LHS = new NumberLiteral { Value = 1d },
                    RHS = new NumberLiteral { Value = 1d },
                    ReturnBool = true
                }
        },
        new()
        {
            Input = "1 > bool 1",
            Expected =
                new BinaryExpression
                {
                    Op = Operator.Gtr,
                    LHS = new NumberLiteral { Value = 1d },
                    RHS = new NumberLiteral { Value = 1d },
                    ReturnBool = true
                }
        },
        new()
        {
            Input = "1 >= bool 1",
            Expected =
                new BinaryExpression
                {
                    Op = Operator.Gte,
                    LHS = new NumberLiteral { Value = 1d },
                    RHS = new NumberLiteral { Value = 1d },
                    ReturnBool = true
                }
        },
        new()
        {
            Input = "1 < bool 1",
            Expected =
                new BinaryExpression
                {
                    Op = Operator.Lss,
                    LHS = new NumberLiteral { Value = 1d },
                    RHS = new NumberLiteral { Value = 1d },
                    ReturnBool = true
                }
        },
        new()
        {
            Input = "1 <= bool 1",
            Expected =
                new BinaryExpression
                {
                    Op = Operator.Lte,
                    LHS = new NumberLiteral { Value = 1d },
                    RHS = new NumberLiteral { Value = 1d },
                    ReturnBool = true
                }
        },
        new()
        {
            Input = "2 ^ 1",
            Expected =
                new BinaryExpression
                {
                    Op = Operator.Pow,
                    LHS = new NumberLiteral { Value = 2d },
                    RHS = new NumberLiteral { Value = 1d },
                    ReturnBool = false
                }
        },
        new()
        {
            Input = "3 ^ (2 ^ 1)",
            Expected = new BinaryExpression
            {
                Op = Operator.Pow,
                LHS = new NumberLiteral { Value = 3d },
                RHS = new BinaryExpression
                {
                    Op = Operator.Pow,
                    LHS = new NumberLiteral { Value = 2d },
                    RHS = new NumberLiteral { Value = 1d },
                    ReturnBool = false
                },
                ReturnBool = false
            }
        },

        #endregion
        #region Vector binary operations

        new()
        {
            Input = "foo * bar",
            Expected = new BinaryExpression
            {
                Op = Operator.Mul,
                LHS = new VectorSelector
                {
                    Name = "foo",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                RHS = new VectorSelector
                {
                    Name = "bar",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "bar", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                VectorMatching =
                    new VectorMatching
                    {
                        Cardinality = VectorMatchCardinality.OneToOne, MatchingLabels = [], On = false, Include = []
                    }
            }
        },
        new()
        {
            Input = "foo == 1",
            Expected = new BinaryExpression
            {
                Op = Operator.Eql,
                LHS = new VectorSelector
                {
                    Name = "foo",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                RHS = new NumberLiteral { Value = 1d }
            }
        },
        new()
        {
            Input = "foo == bool 1",
            Expected = new BinaryExpression
            {
                Op = Operator.Eql,
                LHS = new VectorSelector
                {
                    Name = "foo",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                RHS = new NumberLiteral { Value = 1d },
                ReturnBool = true
            }
        },
        new()
        {
            Input = "2.5/ bar",
            Expected = new BinaryExpression
            {
                Op = Operator.Div,
                LHS = new NumberLiteral { Value = 2.5 },
                RHS = new VectorSelector
                {
                    Name = "bar",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "bar", LabelMatcherType.Equal)
                    ],
                    Series = []
                }
            }
        },
        new()
        {
            Input = "foo and bar",
            Expected = new BinaryExpression
            {
                Op = Operator.And,
                LHS = new VectorSelector
                {
                    Name = "foo",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                RHS = new VectorSelector
                {
                    Name = "bar",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "bar", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                VectorMatching =
                    new VectorMatching
                    {
                        Cardinality = VectorMatchCardinality.ManyToMany,
                        MatchingLabels = [],
                        On = false,
                        Include = []
                    }
            }
        },
        new()
        {
            Input = "foo or bar",
            Expected = new BinaryExpression
            {
                Op = Operator.Or,
                LHS = new VectorSelector
                {
                    Name = "foo",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                RHS = new VectorSelector
                {
                    Name = "bar",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "bar", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                VectorMatching =
                    new VectorMatching
                    {
                        Cardinality = VectorMatchCardinality.ManyToMany,
                        MatchingLabels = [],
                        On = false,
                        Include = []
                    }
            }
        },
        new()
        {
            Input = "foo unless bar",
            Expected = new BinaryExpression
            {
                Op = Operator.Unless,
                LHS = new VectorSelector
                {
                    Name = "foo",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                RHS = new VectorSelector
                {
                    Name = "bar",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "bar", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                VectorMatching =
                    new VectorMatching
                    {
                        Cardinality = VectorMatchCardinality.ManyToMany,
                        MatchingLabels = [],
                        On = false,
                        Include = []
                    }
            }
        },
        // Test and/or precedence and reassigning of operands.
        new()
        {
            Input = "foo + bar or bla and blub",
            Expected = new BinaryExpression
            {
                Op = Operator.Or,
                LHS = new BinaryExpression
                {
                    Op = Operator.Add,
                    LHS = new VectorSelector
                    {
                        Name = "foo",
                        LabelMatchers =
                        [
                            new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                        ],
                        Series = []
                    },
                    RHS = new VectorSelector
                    {
                        Name = "bar",
                        LabelMatchers =
                        [
                            new LabelMatcher(Labels.MetricName, "bar", LabelMatcherType.Equal)
                        ],
                        Series = []
                    },
                    VectorMatching =
                        new VectorMatching
                        {
                            Cardinality = VectorMatchCardinality.OneToOne,
                            MatchingLabels = [],
                            On = false,
                            Include = []
                        }
                },
                RHS = new BinaryExpression
                {
                    Op = Operator.And,
                    LHS = new VectorSelector
                    {
                        Name = "bla",
                        LabelMatchers =
                        [
                            new LabelMatcher(Labels.MetricName, "bla", LabelMatcherType.Equal)
                        ],
                        Series = []
                    },
                    RHS = new VectorSelector
                    {
                        Name = "blub",
                        LabelMatchers =
                        [
                            new LabelMatcher(Labels.MetricName, "blub", LabelMatcherType.Equal)
                        ],
                        Series = []
                    },
                    VectorMatching =
                        new VectorMatching
                        {
                            Cardinality = VectorMatchCardinality.ManyToMany,
                            MatchingLabels = [],
                            On = false,
                            Include = []
                        }
                },
                VectorMatching =
                    new VectorMatching
                    {
                        Cardinality = VectorMatchCardinality.ManyToMany,
                        MatchingLabels = [],
                        On = false,
                        Include = []
                    }
            }
        },
        // Test and/or/unless precedence.
        new()
        {
            Input = "foo and bar unless baz or qux",
            Expected = new BinaryExpression
            {
                Op = Operator.Or,
                LHS = new BinaryExpression
                {
                    Op = Operator.Unless,
                    LHS = new BinaryExpression
                    {
                        Op = Operator.And,
                        LHS = new VectorSelector
                        {
                            Name = "foo",
                            LabelMatchers =
                            [
                                new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                            ],
                            Series = []
                        },
                        RHS = new VectorSelector
                        {
                            Name = "bar",
                            LabelMatchers =
                            [
                                new LabelMatcher(Labels.MetricName, "bar", LabelMatcherType.Equal)
                            ],
                            Series = []
                        },
                        VectorMatching =
                            new VectorMatching
                            {
                                Cardinality = VectorMatchCardinality.ManyToMany,
                                MatchingLabels = [],
                                On = false,
                                Include = []
                            }
                    },
                    RHS = new VectorSelector
                    {
                        Name = "baz",
                        LabelMatchers =
                        [
                            new LabelMatcher(Labels.MetricName, "baz", LabelMatcherType.Equal)
                        ],
                        Series = []
                    },
                    VectorMatching =
                        new VectorMatching
                        {
                            Cardinality = VectorMatchCardinality.ManyToMany,
                            MatchingLabels = [],
                            On = false,
                            Include = []
                        }
                },
                RHS = new VectorSelector
                {
                    Name = "qux",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "qux", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                VectorMatching =
                    new VectorMatching
                    {
                        Cardinality = VectorMatchCardinality.ManyToMany,
                        MatchingLabels = [],
                        On = false,
                        Include = []
                    }
            }
        },

        // Test precedence and reassigning of operands.
        new()
        {
            Input = "bar + on(foo) bla / on(baz, buz) group_right(test) blub",
            Expected = new BinaryExpression
            {
                Op = Operator.Add,
                LHS = new VectorSelector
                {
                    Name = "bar",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "bar", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                RHS = new BinaryExpression
                {
                    Op = Operator.Div,
                    LHS = new VectorSelector
                    {
                        Name = "bla",
                        LabelMatchers =
                        [
                            new LabelMatcher(Labels.MetricName, "bla", LabelMatcherType.Equal)
                        ],
                        Series = []
                    },
                    RHS = new VectorSelector
                    {
                        Name = "blub",
                        LabelMatchers =
                        [
                            new LabelMatcher(Labels.MetricName, "blub", LabelMatcherType.Equal)
                        ],
                        Series = []
                    },
                    VectorMatching =
                        new VectorMatching
                        {
                            Cardinality = VectorMatchCardinality.OneToMany,
                            MatchingLabels = ["baz", "buz"],
                            On = true,
                            Include = ["test"]
                        }
                },
                VectorMatching =
                    new VectorMatching
                    {
                        Cardinality = VectorMatchCardinality.OneToOne,
                        MatchingLabels = ["foo"],
                        On = true,
                        Include = []
                    }
            }
        },
        new()
        {
            Input = "foo * on(test,blub) bar",
            Expected = new BinaryExpression
            {
                Op = Operator.Mul,
                LHS = new VectorSelector
                {
                    Name = "foo",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                RHS = new VectorSelector
                {
                    Name = "bar",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "bar", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                VectorMatching =
                    new VectorMatching
                    {
                        Cardinality = VectorMatchCardinality.OneToOne,
                        MatchingLabels = ["test", "blub"],
                        On = true,
                        Include = []
                    }
            }
        },
        new()
        {
            Input = "foo * on(test,blub) group_left bar",
            Expected = new BinaryExpression
            {
                Op = Operator.Mul,
                LHS = new VectorSelector
                {
                    Name = "foo",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                RHS = new VectorSelector
                {
                    Name = "bar",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "bar", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                VectorMatching =
                    new VectorMatching
                    {
                        Cardinality = VectorMatchCardinality.ManyToOne,
                        MatchingLabels = ["test", "blub"],
                        On = true,
                        Include = []
                    }
            }
        },
        new()
        {
            Input = "foo and on(test,blub) bar",
            Expected = new BinaryExpression
            {
                Op = Operator.And,
                LHS = new VectorSelector
                {
                    Name = "foo",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                RHS = new VectorSelector
                {
                    Name = "bar",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "bar", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                VectorMatching =
                    new VectorMatching
                    {
                        Cardinality = VectorMatchCardinality.ManyToMany,
                        MatchingLabels = ["test", "blub"],
                        On = true,
                        Include = []
                    }
            }
        },
        new()
        {
            Input = "foo and on() bar",
            Expected = new BinaryExpression
            {
                Op = Operator.And,
                LHS = new VectorSelector
                {
                    Name = "foo",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                RHS = new VectorSelector
                {
                    Name = "bar",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "bar", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                VectorMatching =
                    new VectorMatching
                    {
                        Cardinality = VectorMatchCardinality.ManyToMany,
                        MatchingLabels = [],
                        On = true,
                        Include = []
                    }
            }
        },
        new()
        {
            Input = "foo and ignoring(test,blub) bar",
            Expected = new BinaryExpression
            {
                Op = Operator.And,
                LHS = new VectorSelector
                {
                    Name = "foo",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                RHS = new VectorSelector
                {
                    Name = "bar",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "bar", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                VectorMatching =
                    new VectorMatching
                    {
                        Cardinality = VectorMatchCardinality.ManyToMany,
                        MatchingLabels = ["test", "blub"],
                        On = false,
                        Include = []
                    }
            }
        },
        new()
        {
            Input = "foo and ignoring() bar",
            Expected = new BinaryExpression
            {
                Op = Operator.And,
                LHS = new VectorSelector
                {
                    Name = "foo",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                RHS = new VectorSelector
                {
                    Name = "bar",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "bar", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                VectorMatching =
                    new VectorMatching
                    {
                        Cardinality = VectorMatchCardinality.ManyToMany,
                        MatchingLabels = [],
                        On = false,
                        Include = []
                    }
            }
        },
        new()
        {
            Input = "foo unless on(test,blub) bar",
            Expected = new BinaryExpression
            {
                Op = Operator.Unless,
                LHS = new VectorSelector
                {
                    Name = "foo",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                RHS = new VectorSelector
                {
                    Name = "bar",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "bar", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                VectorMatching =
                    new VectorMatching
                    {
                        Cardinality = VectorMatchCardinality.ManyToMany,
                        MatchingLabels = ["test", "blub"],
                        On = true,
                        Include = []
                    }
            }
        },
        new()
        {
            Input = "foo unless on(bar) baz",
            Expected = new BinaryExpression
            {
                Op = Operator.Unless,
                LHS = new VectorSelector
                {
                    Name = "foo",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                RHS = new VectorSelector
                {
                    Name = "baz",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "baz", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                VectorMatching =
                    new VectorMatching
                    {
                        Cardinality = VectorMatchCardinality.ManyToMany,
                        MatchingLabels = ["bar"],
                        On = true,
                        Include = []
                    }
            }
        },
        new()
        {
            Input = "foo / on(test,blub) group_left(bar) bar",
            Expected = new BinaryExpression
            {
                Op = Operator.Div,
                LHS = new VectorSelector
                {
                    Name = "foo",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                RHS = new VectorSelector
                {
                    Name = "bar",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "bar", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                VectorMatching =
                    new VectorMatching
                    {
                        Cardinality = VectorMatchCardinality.ManyToOne,
                        MatchingLabels = ["test", "blub"],
                        On = true,
                        Include = ["bar"]
                    }
            }
        },
        new()
        {
            Input = "foo / ignoring(test,blub) group_left(bar) bar",
            Expected = new BinaryExpression
            {
                Op = Operator.Div,
                LHS = new VectorSelector
                {
                    Name = "foo",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                RHS = new VectorSelector
                {
                    Name = "bar",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "bar", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                VectorMatching =
                    new VectorMatching
                    {
                        Cardinality = VectorMatchCardinality.ManyToOne,
                        MatchingLabels = ["test", "blub"],
                        Include = ["bar"]
                    }
            }
        },
        new()
        {
            Input = "foo - ignoring(test,blub) group_right(bar,foo) bar",
            Expected = new BinaryExpression
            {
                Op = Operator.Sub,
                LHS = new VectorSelector
                {
                    Name = "foo",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                RHS = new VectorSelector
                {
                    Name = "bar",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "bar", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                VectorMatching =
                    new VectorMatching
                    {
                        Cardinality = VectorMatchCardinality.OneToMany,
                        MatchingLabels = ["test", "blub"],
                        Include = ["bar", "foo"]
                    }
            }
        },
        new()
        {
            Input = "foo - on(test,blub) group_right(bar,foo) bar",
            Expected = new BinaryExpression
            {
                Op = Operator.Sub,
                LHS = new VectorSelector
                {
                    Name = "foo",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                RHS = new VectorSelector
                {
                    Name = "bar",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "bar", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                VectorMatching =
                    new VectorMatching
                    {
                        Cardinality = VectorMatchCardinality.OneToMany,
                        MatchingLabels = ["test", "blub"],
                        Include = ["bar", "foo"],
                        On = true
                    }
            }
        },

        #endregion

        #region Vector selectors

        new()
        {
            Input = "foo",
            Expected = new VectorSelector
            {
                Name = "foo",
                LabelMatchers =
                    [new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)],
                Series = []
            }
        },
        new()
        {
            Input = "foo offset 5m",
            Expected = new VectorSelector
            {
                Name = "foo",
                Offset = TimeSpan.FromMinutes(5),
                LabelMatchers =
                [
                    new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                ],
                Series = []
            }
        },
        new()
        {
            Input = "foo offset 7m",
            Expected = new VectorSelector
            {
                Name = "foo",
                Offset = TimeSpan.FromMinutes(7),
                LabelMatchers =
                [
                    new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                ],
                Series = []
            }
        },
        new()
        {
            Input = "foo offset 1h30m",
            Expected = new VectorSelector
            {
                Name = "foo",
                Offset = TimeSpan.FromMinutes(90),
                LabelMatchers =
                [
                    new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                ],
                Series = []
            }
        },

        #endregion

        #region Matrix selectors

        new()
        {
            Input = "foo[5m]",
            Expected = new MatrixSelector
            {
                Name = "foo",
                Range = TimeSpan.FromMinutes(5),
                LabelMatchers =
                [
                    new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                ],
                Series = []
            }
        },

        #endregion

        #region Aggregations

        new()
        {
            Input = "sum(foo)",
            Expected = new AggregateExpression
            {
                Op = AggregationOp.Sum,
                Expression = new VectorSelector
                {
                    Name = "foo",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                Grouping = []
            }
        },
        new()
        {
            Input = "sum(foo) by (bar, baz)",
            Expected = new AggregateExpression
            {
                Op = AggregationOp.Sum,
                Expression = new VectorSelector
                {
                    Name = "foo",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                Grouping = ["bar", "baz"]
            }
        },
        new()
        {
            Input = "topk(5, foo)",
            Expected = new AggregateExpression
            {
                Op = AggregationOp.TopK,
                Expression = new VectorSelector
                {
                    Name = "foo",
                    LabelMatchers =
                    [
                        new LabelMatcher(Labels.MetricName, "foo", LabelMatcherType.Equal)
                    ],
                    Series = []
                },
                Parameter = new NumberLiteral { Value = 5 },
                Grouping = []
            }
        },

        #endregion

        #region Functions

        new()
        {
            Input = "rate(foo[5m])",
            Expected = new Call
            {
                Func = new Function
                {
                    Name = FunctionName.Rate,
                    ArgTypes = [PrometheusValueType.Matrix],
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
                ]
            }
        },
        new()
        {
            Input = "histogram_quantile(0.9, sum(rate(foo[5m])) by (le))",
            Expected = new Call
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
                                ArgTypes = [PrometheusValueType.Matrix],
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
                            ]
                        },
                        Grouping = ["le"]
                    }
                ]
            }
        }

        #endregion
    }.Select(x => new object[] { x });

    // In order to have the test cases show up as individual tests,
    // we need to implement IXunitSerializable.
    public class TestCase : IXunitSerializable
    {
        public required string Input { get; set; }
        public required Expression Expected { get; init; }

        public void Deserialize(IXunitSerializationInfo info)
        {
            Input = info.GetValue<string>(nameof(Input));
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(Input), Input);
        }
    }
}
