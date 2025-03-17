// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Mocha.Query.Prometheus.PromQL.Values;

[DebuggerDisplay("TimestampUnixSec: {TimestampUnixSec}, Value: {Value}")]
public class DoublePoint
{
    public long TimestampUnixSec { get; set; }

    public double Value { get; set; }
}
