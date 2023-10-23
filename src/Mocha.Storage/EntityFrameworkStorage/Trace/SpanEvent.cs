// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.


namespace Mocha.Storage.EntityFrameworkStorage.Trace;

public class SpanEvent
{
    public string TraceId { get; set; } = string.Empty;

    public long TimeBucket { get; set; }

    public string EventName { get; set; } = string.Empty;

    public Span Span { get; set; } = default!;
}
