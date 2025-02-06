// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Storage.Prometheus;

namespace Mocha.Query.Prometheus.PromQL.Values;

public class MatrixResult(int capacity) : List<Series>(capacity), IParseResult
{
    public ResultValueType Type => ResultValueType.Matrix;

    public int TotalSamples() => this.Sum(s => s.Points.Count);

    public bool ContainsSameLabelSet()
    {
        var hashSet = new HashSet<Labels>();
        return this.Any(series => !hashSet.Add(series.Metric));
    }
}
