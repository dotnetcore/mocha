// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using LiteDB;
using Mocha.Core.Models.Metrics;

namespace Mocha.Storage.LiteDB.Metadata.Models;

internal class LiteDBMetricMetadata
{
    [BsonId]
    public ObjectId? Id { get; set; }

    [BsonField("m")]
    public required string Metric { get; init; }

    [BsonField("s")]
    public required string ServiceName { get; init; }

    [BsonField("t")]
    public required MochaMetricType Type { get; set; }

    [BsonField("d")]
    public required string Description { get; set; }

    [BsonField("u")]
    public required string Unit { get; set; }
}
