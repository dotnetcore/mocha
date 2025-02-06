// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Mocha.Query.Prometheus.PromQL.Engine;
using Mocha.Query.Prometheus.PromQL.Values;

namespace Mocha.Query.Prometheus.PromQL.Ast;

// Move out from Ast namespace
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
        }
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
