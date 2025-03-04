// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Mocha.Query.Prometheus.PromQL.Engine;
using Mocha.Query.Prometheus.PromQL.Values;

namespace Mocha.Query.Prometheus.PromQL.Ast;

// TODO: handle functions with variable number of arguments
public class Function
{
    private static readonly Dictionary<string, Function> _registeredFunctions = new()
    {
        {
            "abs", new Function
            {
                Name = FunctionName.Abs,
                ArgTypes = [PrometheusValueType.Vector],
                ReturnType = PrometheusValueType.Vector,
                Call = Functions.FuncAbs,
            }
        },
        {
            "absent",
            new Function
            {
                Name = FunctionName.Absent,
                ArgTypes = [PrometheusValueType.Vector],
                ReturnType = PrometheusValueType.Vector,
                Call = Functions.FuncAbsent
            }
        },
        {
            "histogram_quantile",
            new Function
            {
                Name = FunctionName.HistogramQuantile,
                ArgTypes = [PrometheusValueType.Scalar, PrometheusValueType.Vector],
                ReturnType = PrometheusValueType.Vector,
                Call = Functions.FuncHistogramQuantile
            }
        },
        {
            "increase",
            new Function
            {
                Name = FunctionName.Increase,
                ArgTypes = [PrometheusValueType.Matrix],
                ReturnType = PrometheusValueType.Vector,
                Call = Functions.FuncIncrease
            }
        },
        {
            "rate",
            new Function
            {
                Name = FunctionName.Rate,
                ArgTypes = [PrometheusValueType.Matrix],
                ReturnType = PrometheusValueType.Vector,
                Call = Functions.FuncRate
            }
        },
        {
            "avg_over_time",
            new Function
            {
                Name = FunctionName.AvgOverTime,
                ArgTypes = [PrometheusValueType.Matrix],
                ReturnType = PrometheusValueType.Vector,
                Call = Functions.FuncAvgOverTime
            }
        },
        {
            "min_over_time",
            new Function
            {
                Name = FunctionName.MinOverTime,
                ArgTypes = [PrometheusValueType.Matrix],
                ReturnType = PrometheusValueType.Vector,
                Call = Functions.FuncMinOverTime
            }
        },
        {
            "max_over_time",
            new Function
            {
                Name = FunctionName.MaxOverTime,
                ArgTypes = [PrometheusValueType.Matrix],
                ReturnType = PrometheusValueType.Vector,
                Call = Functions.FuncMaxOverTime
            }
        },
        {
            "sum_over_time",
            new Function
            {
                Name = FunctionName.SumOverTime,
                ArgTypes = [PrometheusValueType.Matrix],
                ReturnType = PrometheusValueType.Vector,
                Call = Functions.FuncSumOverTime
            }
        },
        {
            "count_over_time",
            new Function
            {
                Name = FunctionName.CountOverTime,
                ArgTypes = [PrometheusValueType.Matrix],
                ReturnType = PrometheusValueType.Vector,
                Call = Functions.FuncCountOverTime
            }
        },
        {
            "stddev_over_time",
            new Function
            {
                Name = FunctionName.StdDevOverTime,
                ArgTypes = [PrometheusValueType.Matrix],
                ReturnType = PrometheusValueType.Vector,
                Call = Functions.FuncStdDevOverTime
            }
        },
        {
            "stdvar_over_time",
            new Function
            {
                Name = FunctionName.StdVarOverTime,
                ArgTypes = [PrometheusValueType.Matrix],
                ReturnType = PrometheusValueType.Vector,
                Call = Functions.FuncStdVarOverTime
            }
        },
    };

    public FunctionName Name { get; init; }

    public required PrometheusValueType[] ArgTypes { get; init; }

    // TODO: handle functions with variable number of arguments

    public PrometheusValueType ReturnType { get; init; }

    public required Func<IParseResult[], Expression[] /* Arguments */, EvalNodeHelper, VectorResult> Call { get; init; }

    public bool IsSupported(string name, PrometheusValueType[] argTypes) =>
        _registeredFunctions.TryGetValue(name, out var function)
        && argTypes.SequenceEqual(function.ArgTypes);

    public static bool TryGetFunction(string name, [MaybeNullWhen(false)] out Function function) =>
        _registeredFunctions.TryGetValue(name, out function);
}
