// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Models.Metrics;
using Mocha.Core.Storage.Prometheus;

namespace Mocha.Query.Prometheus.PromQL.Values;

public class VectorResult(int capacity = 0) : List<Sample>(capacity), IParseResult
{
    public ResultValueType Type => ResultValueType.Vector;

    public bool ContainsSameLabelSet()
    {
        var hashSet = new HashSet<Labels>();
        return this.Any(sample => !hashSet.Add(sample.Metric));
    }
}
