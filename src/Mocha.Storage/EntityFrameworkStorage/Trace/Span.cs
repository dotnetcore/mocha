// Licensed to the.NET Core Community under one or more agreements.
// The.NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Enums;

namespace Mocha.Storage.EntityFrameworkStorage.Trace;

public class Span
{
    public string TraceId { get; set; } = default!;

    public string SpanId { get; set; } = default!;

    public string SpanName { get; set; } = default!;

    public string ParentSpanId { get; set; } = default!;

    public string StartTime { get; set; } = default!;

    public string EndTime { get; set; } = default!;

    public double Duration { get; set; }

    public int StatusCode { get; set; }

    public string StatusMessage { get; set; } = default!;

    public SpanKindEnum SpanKind { get; set; }

    public string ServiceName { get; set; } = default!;

    public bool TraceFlags { get; set; }

    public bool TraceState { get; set; }

    public ICollection<SpanLink> SpanLinks { get; set; } = new HashSet<SpanLink>();

    public ICollection<SpanAttribute> SpanAttributes { get; set; } = new HashSet<SpanAttribute>();

    public ICollection<SpanEvent> SpanEvents { get; set; } = new HashSet<SpanEvent>();
}
