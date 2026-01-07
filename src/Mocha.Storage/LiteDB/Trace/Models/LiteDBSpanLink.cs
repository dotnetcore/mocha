// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using LiteDB;

namespace Mocha.Storage.LiteDB.Trace.Models;

public class LiteDBSpanLink
{
    [BsonField("lti")]
    public required string LinkedTraceId { get; init; }

    [BsonField("lsi")]
    public required string LinkedSpanId { get; init; }

    [BsonField("a")]
    public required IEnumerable<LiteDBAttribute> Attributes { get; init; }

    [BsonField("lts")]
    public required string LinkedTraceState { get; init; }

    [BsonField("ltf")]
    public uint LinkedTraceFlags { get; init; }
}
