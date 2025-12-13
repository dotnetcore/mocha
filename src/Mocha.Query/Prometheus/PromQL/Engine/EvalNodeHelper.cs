// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Models.Metrics;
using Mocha.Core.Storage.Prometheus;
using Mocha.Query.Prometheus.PromQL.Values;

namespace Mocha.Query.Prometheus.PromQL.Engine;

// EvalNodeHelper stores extra information and caches for evaluating a single node across steps.
public class EvalNodeHelper
{
    public long TimestampUnixSec { get; set; }

    public required VectorResult Output { get; set; }

    // For binary vector matching.
    public Dictionary<Labels, HashSet<Labels>>? MatchedSignatures { get; set; }

    public Dictionary<(Labels LHS, Labels RHS), Labels>? ResultMetric { get; set; }
}
