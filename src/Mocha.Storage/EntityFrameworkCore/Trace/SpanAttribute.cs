// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Storage.EntityFrameworkCore.Trace;

public class SpanAttribute
{
    public string AttributeKey { get; set; } = string.Empty;

    public string AttributeValue { get; set; } = string.Empty;

    public long TimeBucket { get; set; }

    public string TraceId { get; set; } = string.Empty;

    public string SpanId { get; set; } = string.Empty;

    public Span Span { get; set; } = default!;
}
