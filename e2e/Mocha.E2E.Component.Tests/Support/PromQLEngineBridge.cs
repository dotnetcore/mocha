// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Mocha.Core.Storage.Prometheus;
using Mocha.Query.Prometheus.PromQL.Engine;
using Mocha.Query.Prometheus.PromQL.Values;

namespace Mocha.E2E.Component.Tests.Support;

/// <summary>
/// Constructs the production PromQL engine over a supplied <see cref="IPrometheusMetricsReader"/>.
/// </summary>
/// <remarks>
/// <c>PromQLEngine</c> and its parser <c>MochaPromQLParserParser</c> are <c>internal</c> to
/// <c>Mocha.Query</c> and only exposed to <c>Mocha.Query.Tests</c> via <c>InternalsVisibleTo</c>.
/// This component-test assembly is a different assembly, and the src production code is out of scope
/// to modify, so we reach the same engine the Query service uses through its public
/// <see cref="IPromQLEngine"/> surface plus reflection on the two internal ctors. The result types
/// (<see cref="IParseResult"/>, <see cref="VectorResult"/>) are public.
/// </remarks>
internal static class PromQLEngineBridge
{
    private const string EngineTypeName = "Mocha.Query.Prometheus.PromQL.Engine.PromQLEngine";
    private const string ParserTypeName = "Mocha.Query.Prometheus.PromQL.Engine.MochaPromQLParserParser";

    public static IPromQLEngine Create(IPrometheusMetricsReader metricsReader, PromQLEngineOptions? options = null)
    {
        var assembly = typeof(IPromQLEngine).Assembly;

        var parserType = assembly.GetType(ParserTypeName)
            ?? throw new InvalidOperationException($"Type '{ParserTypeName}' not found in {assembly.FullName}.");
        var engineType = assembly.GetType(EngineTypeName)
            ?? throw new InvalidOperationException($"Type '{EngineTypeName}' not found in {assembly.FullName}.");

        var parser = (IPromQLParser)(Activator.CreateInstance(parserType, nonPublic: true)
            ?? throw new InvalidOperationException($"Could not instantiate '{ParserTypeName}'."));

        var optionsAccessor = Options.Create(options ?? new PromQLEngineOptions());

        var engine = Activator.CreateInstance(engineType, parser, metricsReader, optionsAccessor)
            ?? throw new InvalidOperationException($"Could not instantiate '{EngineTypeName}'.");

        return (IPromQLEngine)engine;
    }
}
