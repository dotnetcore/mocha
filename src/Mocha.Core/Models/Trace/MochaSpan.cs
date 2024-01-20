// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Models.Trace;

public class MochaSpan
{
    public required string TraceId { get; init; }

    public required string SpanId { get; init; }

    public required string SpanName { get; init; } = string.Empty;

    public required string ParentSpanId { get; init; } = string.Empty;

    public ulong StartTimeUnixNano { get; init; }

    public ulong EndTimeUnixNano { get; init; }

    public ulong DurationNanoseconds { get; init; }

    public MochaSpanStatusCode? StatusCode { get; init; }

    public string? StatusMessage { get; init; }

    public MochaSpanKind SpanKind { get; init; }

    public required MochaResource Resource { get; init; }

    public uint TraceFlags { get; init; }

    public string? TraceState { get; init; }

    public required IEnumerable<MochaSpanLink> Links { get; init; }

    public required IEnumerable<MochaAttribute> Attributes { get; init; }

    public required IEnumerable<MochaSpanEvent> Events { get; init; }
}
