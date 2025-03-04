// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Mocha.Core.Extensions;
using Mocha.Core.Storage.Prometheus;
using Mocha.Core.Storage.Prometheus.Metrics;
using Mocha.Query.Prometheus.PromQL.Exceptions;

namespace Mocha.Query.Prometheus.PromQL.Ast;

public class AstBuilder : PromQLParserBaseVisitor<Expression>
{
    #region Operators

    #region Unary Operators

    public override Expression VisitUnaryNode(PromQLParser.UnaryNodeContext context)
    {
        var opType = (Operator)((TerminalNodeImpl)context.GetChild(0).GetChild(0)).Symbol.Type;
        var expr = Visit(context.GetChild(1));

        return new UnaryExpression
        {
#if DEBUG
            ExpressionText = BuildExpressionText(context),
#endif

            Operator = opType,
            Expression = expr
        };
    }

    #endregion

    #region Binary Operators

    public override Expression VisitPowNode(PromQLParser.PowNodeContext context) => ParseBinaryExpression(context);

    public override Expression VisitMultNode(PromQLParser.MultNodeContext context) => ParseBinaryExpression(context);

    public override Expression VisitAddNode(PromQLParser.AddNodeContext context) => ParseBinaryExpression(context);

    public override Expression VisitCompareNode(PromQLParser.CompareNodeContext context) =>
        ParseBinaryExpression(context);

    public override Expression VisitAndUnlessNode(PromQLParser.AndUnlessNodeContext context) =>
        ParseBinaryExpression(context);

    public override Expression VisitOrNode(PromQLParser.OrNodeContext context) => ParseBinaryExpression(context);

    public override Expression VisitVectorMatchNode(PromQLParser.VectorMatchNodeContext context) =>
        ParseBinaryExpression(context);

    #endregion

    #endregion

    #region Vectors

    public override Expression VisitFunction_(PromQLParser.Function_Context context)
    {
        var funcName = context.FUNCTION().GetText();
        if (Function.TryGetFunction(funcName, out var function) == false)
        {
            throw new InvalidOperationException($"Unsupported function: {funcName}");
        }

        var args = context.parameter().Select(Visit).ToArray();

        for (var i = 0; i < args.Length; i++)
        {
            var expectedType = function.ArgTypes[i];
            ThrowIfTypeNotMatch(args[i], expectedType, $"call to function \"{funcName}\"");
        }

        return new Call
        {
#if DEBUG
            ExpressionText = BuildExpressionText(context),
#endif
            Func = function,
            Args = args
        };
    }

    public override Expression VisitAggregation(PromQLParser.AggregationContext context)
    {
        // parameter is only required for count_values, quantile, topk, bottomk, limitk and limit_ratio
        // https://prometheus.io/docs/prometheus/latest/querying/operators/#aggregation-operators
        var op = context.AGGREGATION_OPERATOR().GetText().ToLowerInvariant();

        var parameterContexts = context.parameterList().parameter();
        // if parameter is provided, it will be the first element in the list
        var parameter = parameterContexts.Length switch
        {
            2 => Visit(parameterContexts[0]),
            _ => null
        };
        var expression = Visit(parameterContexts[^1]);

        var grouping = new HashSet<string>();
        var without = false;

        if (context.by() != null)
        {
            grouping = context.by().labelNameList().labelName().Select(label => label.GetText()).ToHashSet();
        }
        else if (context.without() != null)
        {
            without = true;
            grouping = context.without().labelNameList().labelName().Select(label => label.GetText()).ToHashSet();
        }

        return new AggregateExpression
        {
#if DEBUG
            ExpressionText = BuildExpressionText(context),
#endif
            Op = MapAggregationOp(op),
            Expression = expression,
            Parameter = parameter,
            Grouping = grouping,
            Without = without
        };

        static AggregationOp MapAggregationOp(string op) => op switch
        {
            // TODO: optimize this
            "sum" => AggregationOp.Sum,
            "min" => AggregationOp.Min,
            "max" => AggregationOp.Max,
            "avg" => AggregationOp.Avg,
            "group" => AggregationOp.Group,
            "stddev" => AggregationOp.StdDev,
            "stdvar" => AggregationOp.StdVar,
            "count" => AggregationOp.Count,
            "count_values" => AggregationOp.CountValues,
            "bottomk" => AggregationOp.BottomK,
            "topk" => AggregationOp.TopK,
            "quantile" => AggregationOp.Quantile,
            _ => throw new InvalidOperationException($"Unsupported aggregation operator: {op}")
        };
    }

    public override Expression VisitVectorSelector(PromQLParser.VectorSelectorContext context)
    {
        var metricName = context.METRIC_NAME()?.GetText();
        var labelMatchers = new List<LabelMatcher>();
        if (!string.IsNullOrWhiteSpace(metricName))
        {
            labelMatchers.Add(new LabelMatcher(Labels.MetricName, metricName, LabelMatcherType.Equal));
        }

        if (context.labelMatcherList() != null)
        {
            labelMatchers.AddRange(context.labelMatcherList().labelMatcher().Select(ParseLabelMatcher));
        }

        return new VectorSelector
        {
#if DEBUG
            ExpressionText = BuildExpressionText(context),
#endif
            Name = metricName,
            LabelMatchers = labelMatchers,
            Series = []
        };
    }

    public override Expression VisitMatrixSelector(PromQLParser.MatrixSelectorContext context)
    {
        var vectorSelector = VisitVectorSelector(context.vectorSelector()) as VectorSelector ??
                             throw new InvalidOperationException("Invalid vector selector");
        var rangeExpr = context.TIME_RANGE().GetText().Trim("[]".ToCharArray());
        if (!PromQLUtils.TryParseDuration(rangeExpr, out var range))
        {
            throw new PromQLIllegalExpressionException(
                $"invalid range expression {rangeExpr} in \"{context.GetText()}\"");
        }

        return new MatrixSelector
        {
#if DEBUG
            ExpressionText = BuildExpressionText(context),
#endif
            Name = vectorSelector.Name,
            Range = range,
            LabelMatchers = vectorSelector.LabelMatchers,
            Offset = vectorSelector.Offset,
            Series = []
        };
    }

    public override Expression VisitOffset(PromQLParser.OffsetContext context)
    {
        if (!PromQLUtils.TryParseDuration(context.DURATION().GetText(), out var offset))
        {
            throw new PromQLIllegalExpressionException(
                $"invalid offset expression {context.DURATION().GetText()} in {context.GetText()}");
        }

        if (context.vectorSelector() != null)
        {
            var vectorSelector = VisitVectorSelector(context.vectorSelector()) as VectorSelector ??
                                 throw new InvalidOperationException("Invalid vector selector");
            vectorSelector.Offset = offset;
            return vectorSelector;
        }

        if (context.matrixSelector() != null)
        {
            var matrixSelector = VisitMatrixSelector(context.matrixSelector()) as MatrixSelector ??
                                 throw new InvalidOperationException("Invalid matrix selector");
            matrixSelector.Offset = offset;
            return matrixSelector;
        }

        throw new InvalidOperationException("Invalid offset");
    }

    public override Expression VisitLiteral(PromQLParser.LiteralContext context)
    {
        if (context.NUMBER() != null)
        {
            var numberText = context.NUMBER().GetText();
            return new NumberLiteral
            {
#if DEBUG
                ExpressionText = BuildExpressionText(context),
#endif
                Value = double.Parse(numberText),
            };
        }

        if (context.STRING() != null)
        {
            return new StringLiteral
            {
#if DEBUG
                ExpressionText = BuildExpressionText(context),
#endif
                Value = context.STRING().GetText()
            };
        }

        throw new InvalidOperationException("Invalid literal");
    }

    public override Expression VisitParens(PromQLParser.ParensContext context) => Visit(context.vectorOperation());

    #endregion

    #region Private Methods

    private static string BuildExpressionText(ParserRuleContext context) =>
        string.Join(" ", context.children.Select(c => c.GetText()));

    private BinaryExpression ParseBinaryExpression(RuleContext context)
    {
        var lhs = Visit(context.GetChild(0));
        var rhs = Visit(context.GetChild(2));
        var opContext = context.GetChild(1);
        var opType = (Operator)((TerminalNodeImpl)opContext.GetChild(0)).Symbol.Type;

        var returnBool = false;
        PromQLParser.GroupingContext groupingContext;

        if (opType.IsComparisonOperator() && opContext.GetChild(1) is TerminalNodeImpl returnBoolNode)
        {
            returnBool = returnBoolNode.GetText() == "bool";

            groupingContext = (PromQLParser.GroupingContext)opContext.GetChild(2);
        }
        else
        {
            groupingContext = (PromQLParser.GroupingContext)opContext.GetChild(1);
        }

        VectorMatching? vectorMatching = null;
        if (lhs.Type == PrometheusValueType.Vector && rhs.Type == PrometheusValueType.Vector)
        {
            var isSetOperator = opType.IsSetOperator();
            if (groupingContext == null)
            {
                vectorMatching = new VectorMatching
                {
                    Cardinality =
                        isSetOperator ? VectorMatchCardinality.ManyToMany : VectorMatchCardinality.OneToOne,
                    MatchingLabels = [],
                    On = false,
                    Include = []
                };
            }
            else
            {
                vectorMatching = ParseVectorMatching(isSetOperator, groupingContext);
            }
        }

#if DEBUG
        var childCount = context.ChildCount;
        var children = new IParseTree[childCount];
        for (var i = 0; i < childCount; i++)
        {
            children[i] = context.GetChild(i);
        }
#endif

        return new BinaryExpression
        {
#if DEBUG
            ExpressionText = string.Join(" ", children.Select(c => c.GetText())),
#endif
            Op = opType,
            LHS = lhs,
            RHS = rhs,
            VectorMatching = vectorMatching,
            ReturnBool = returnBool,
        };
    }

    private static VectorMatching ParseVectorMatching(bool isSetOperator, PromQLParser.GroupingContext context)
    {
        var on = context.on_() != null;
        var matchingLabels =
            ParseLabelNames(on ? context.on_().labelNameList() : context.ignoring().labelNameList());

        var matchCardinality = isSetOperator ? VectorMatchCardinality.ManyToMany : VectorMatchCardinality.OneToOne;

        PromQLParser.LabelNameListContext? labelNameListContext = null;
        if (context.groupLeft() != null)
        {
            matchCardinality = VectorMatchCardinality.ManyToOne;
            labelNameListContext = context.groupLeft().labelNameList();
        }
        else if (context.groupRight() != null)
        {
            matchCardinality = VectorMatchCardinality.OneToMany;
            labelNameListContext = context.groupRight().labelNameList();
        }

        var include = labelNameListContext == null ? [] : ParseLabelNames(labelNameListContext);

        return new VectorMatching
        {
            Cardinality = matchCardinality,
            MatchingLabels = matchingLabels,
            On = on,
            Include = include,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static HashSet<string> ParseLabelNames(PromQLParser.LabelNameListContext context) =>
        context.labelName().Select(label => label.GetText()).ToHashSet();

    private static LabelMatcher ParseLabelMatcher(PromQLParser.LabelMatcherContext context)
    {
        var labelName = context.labelName().GetText();
        var labelValue = context.STRING().GetText().Trim('"');
        var matcherOperator = context.labelMatcherOperator().GetText();

        var matcherType = matcherOperator switch
        {
            "=" => LabelMatcherType.Equal,
            "!=" => LabelMatcherType.NotEqual,
            "=~" => LabelMatcherType.RegexMatch,
            "!~" => LabelMatcherType.RegexNotMatch,
            _ => throw new InvalidOperationException($"Unknown label matcher operator: {matcherOperator}")
        };

        return new LabelMatcher(labelName, labelValue, matcherType);
    }

    private static void ThrowIfTypeNotMatch(Expression node, PrometheusValueType expected, string context)
    {
        if (node.Type != expected)
        {
            throw new PromQLIllegalExpressionException(
                $"expected type {expected.GetDescription()} in {context}, got {node.Type.GetDescription()}");
        }
    }

    #endregion
}
