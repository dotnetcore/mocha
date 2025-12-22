// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using LiteDB;

namespace Mocha.Storage.LiteDB.Trace.Models;

public class LiteDBResource
{
    [BsonField("s")]
    public required string ServiceName { get; init; }

    [BsonField("i")]
    public required string ServiceInstanceId { get; init; }

    [BsonField("a")]
    public required IEnumerable<LiteDBAttribute> Attributes { get; init; }
}
