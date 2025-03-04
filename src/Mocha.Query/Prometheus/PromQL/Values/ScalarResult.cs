// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Query.Prometheus.PromQL.Values;

internal class ScalarResult : IParseResult
{
    public ResultValueType Type => ResultValueType.Scalar;

    public long TimestampUnixSec { get; init; }

    public double Value { get; init; }
}
