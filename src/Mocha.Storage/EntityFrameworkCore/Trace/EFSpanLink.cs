// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Storage.EntityFrameworkCore.Trace;

public class EFSpanLink
{
    public long Id { get; init; }

    public required string TraceId { get; init; }

    public required string SpanId { get; init; }

    public int Index { get; init; }

    public required string LinkedTraceId { get; init; }

    public required string LinkedSpanId { get; init; }

    public required string LinkedTraceState { get; init; }

    public uint LinkedTraceFlags { get; init; }
}
