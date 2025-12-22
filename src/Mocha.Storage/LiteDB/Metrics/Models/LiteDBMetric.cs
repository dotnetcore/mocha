// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using LiteDB;
using Mocha.Core.Models.Metrics;

namespace Mocha.Storage.LiteDB.Metrics.Models;

public class LiteDBMetric
{
    [BsonId]
    public ObjectId? Id { get; set; }

    [BsonField("n")]
    public required string Name { get; init; }

    [BsonField("t")]
    public MochaMetricType Type { get; set; }

    [BsonField("u")]
    public required string Unit { get; init; }

    /// <summary>
    /// Labels are stored as an array of strings in "key=value" format.
    /// </summary>
    [BsonField("l")]
    public required string[] Labels { get; init; }

    [BsonField("ln")]
    public required string[] LabelNames { get; init; }

    [BsonField("v")]
    public required double Value { get; init; }

    [BsonField("ts")]
    public required long TimestampUnixNano { get; init; }
}
