// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Storage.EntityFrameworkCore.Trace;

public class EFSpanAttribute
{
    public long Id { get; set; }

    public string AttributeKey { get; set; } = string.Empty;

    public string AttributeValue { get; set; } = string.Empty;

    public string TraceId { get; set; } = string.Empty;

    public string SpanId { get; set; } = string.Empty;

    public EFSpan Span { get; set; } = default!;
}
