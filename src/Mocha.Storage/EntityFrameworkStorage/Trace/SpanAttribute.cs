// Licensed to the.NET Core Community under one or more agreements.
// The.NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Storage.EntityFrameworkStorage.Trace;

public class SpanAttribute
{
    public string AttributeKey { get; private set; } = default!;

    public string AttributeValue { get; private set; } = default!;

    public long TimeBucket { get; private set; }

    public string TraceId { get; private set; } = default!;

    public string SpanId { get; private set; } = default!;

    public Span Span { get; set; } = default!;
}
