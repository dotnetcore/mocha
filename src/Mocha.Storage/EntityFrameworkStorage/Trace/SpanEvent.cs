// Licensed to the.NET Core Community under one or more agreements.
// The.NET Core Community licenses this file to you under the MIT license.


namespace Mocha.Storage.EntityFrameworkStorage.Trace;

public class SpanEvent
{
    public string TraceId { get; private set; } = default!;

    public long TimeBucket { get; private set; }

    public string EventName { get; private set; } = default!;

    public Span Span { get; set; } = default!;
}
