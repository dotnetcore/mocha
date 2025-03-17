// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Storage.Prometheus.Metrics;
using Mocha.Query.Prometheus.PromQL.Ast;

namespace Mocha.Query.Prometheus.PromQL.Engine;

public interface IPromQLParser
{
    Expression ParseExpression(string query);
    IEnumerable<LabelMatcher> ParseMetricSelector(string query);
}
