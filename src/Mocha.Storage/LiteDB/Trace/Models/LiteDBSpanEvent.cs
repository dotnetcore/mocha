// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using LiteDB;
using Mocha.Storage.LiteDB.Trace.Models;

namespace Mocha.Storage.LiteDB.Trace;

public class LiteDBSpanEvent
{
    [BsonField("n")]
    public required string Name { get; init; }

    [BsonField("a")]
    public required IEnumerable<LiteDBAttribute> Attributes { get; init; }

    [BsonField("t")]
    public ulong TimestampUnixNano { get; init; }
}
