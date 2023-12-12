// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Storage.EntityFrameworkCore.Trace;

public class EFSpanLink
{
    public long Id { get; set; }

    public string TraceId { get; set; } = string.Empty;

    public string SpanId { get; set; } = string.Empty;

    public string LinkedSpanId { get; set; } = string.Empty;

    public string TraceState { get; set; } = string.Empty;

    public uint Flags { get; set; }

    public EFSpan Span { get; set; } = default!;
}
