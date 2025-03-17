// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Models.Metrics;

namespace Mocha.Storage.EntityFrameworkCore.Metadata.Readers;

public static class EFToPrometheusConversionExtensions
{
    public static string ToPrometheusType(this MochaMetricType type)
    {
        return type switch
        {
            MochaMetricType.Gauge => "gauge",
            MochaMetricType.Sum => "counter",
            MochaMetricType.Histogram => "histogram",
            MochaMetricType.ExponentialHistogram => "histogram",
            MochaMetricType.Summary => "summary",
            _ => "untyped"
        };
    }
}
