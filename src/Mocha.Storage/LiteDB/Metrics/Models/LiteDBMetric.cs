// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using LiteDB;
using Mocha.Core.Models.Metrics;

namespace Mocha.Storage.LiteDB.Metrics.Models;

public class LiteDBMetric
{
    public ObjectId? Id { get; set; }

    public required string Name { get; init; }

    public MochaMetricType Type { get; set; }

    public required string Unit { get; init; }

    /// <summary>
    /// Labels are stored as an array of strings in "key=value" format.
    /// </summary>
    public required string[] Labels { get; init; }

    public required string[] LabelNames { get; init; }

    public required double Value { get; init; }

    public required long TimestampUnixNano { get; init; }
}
