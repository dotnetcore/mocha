// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.


namespace Mocha.Storage.EntityFrameworkCore.Trace;

public class EFSpanEvent
{
    public long Id { get; set; }

    public string TraceId { get; set; } = string.Empty;

    public long TimeBucket { get; set; }

    public string EventName { get; set; } = string.Empty;

    public EFSpan Span { get; set; } = default!;
}
