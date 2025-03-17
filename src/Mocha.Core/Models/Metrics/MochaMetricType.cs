// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Models.Metrics;

public enum MochaMetricType
{
    None = 0,
    Gauge = 5,
    Sum = 7,
    Histogram = 9,
    ExponentialHistogram = 10,
    Summary = 11
}
