// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Mocha.Query.Prometheus.PromQL.Values;

[DebuggerDisplay("{" + nameof(TimestampUnixSeconds) + "}: {" + nameof(Value) + "}")]
public class DoublePoint
{
    public long TimestampUnixSeconds { get; set; }

    public double Value { get; set; }
}
