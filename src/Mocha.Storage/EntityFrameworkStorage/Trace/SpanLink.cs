// Licensed to the.NET Core Community under one or more agreements.
// The.NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Storage.EntityFrameworkStorage.Trace;

public class SpanLink
{
    public string TraceId { get; private set; } = default!;

    public string SpanId { get; private set; } = default!;

    public string LinkedSpanId { get; private set; } = default!;

    public string TraceState { get; private set; } = default!;

    public bool Flags { get; private set; }

    public Span Span { get; set; } = default!;
}
