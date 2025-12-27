// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using LiteDB;

namespace Mocha.Storage.LiteDB.Trace.Models;

public class LiteDBSpan
{
    [BsonField("si")]
    public required string SpanId { get; init; }

    [BsonField("ti")]
    public required string TraceId { get; init; }

    [BsonField("n")]
    public required string SpanName { get; init; }

    [BsonField("psi")]
    public required string ParentSpanId { get; init; }

    [BsonField("sts")]
    public ulong StartTimeUnixNano { get; init; }

    [BsonField("ets")]
    public ulong EndTimeUnixNano { get; init; }

    [BsonField("durn")]
    public ulong DurationNanoseconds { get; init; }

    [BsonField("sc")]
    public LiteDBSpanStatusCode? StatusCode { get; init; }

    [BsonField("sm")]
    public string? StatusMessage { get; init; }

    [BsonField("k")]
    public LiteDBSpanKind SpanKind { get; init; }

    [BsonField("sn")]
    public required string ServiceName { get; init; }

    [BsonField("sii")]
    public required string ServiceInstanceId { get; init; }

    [BsonField("r")]
    public required LiteDBResource Resource { get; init; }

    [BsonField("tf")]
    public uint TraceFlags { get; init; }

    [BsonField("ts")]
    public string? TraceState { get; init; }

    [BsonField("l")]
    public required IEnumerable<LiteDBSpanLink> Links { get; init; }

    [BsonField("a")]
    public required IEnumerable<LiteDBAttribute> Attributes { get; init; }

    // Stored as "key=value" strings for efficient querying
    // Attributes from both span and resource are included
    [BsonField("avs")]
    public required IEnumerable<string> AttributeKeyValueStrings { get; init; }

    [BsonField("e")]
    public required IEnumerable<LiteDBSpanEvent> Events { get; init; }
}
