// Licensed to the.NET Core Community under one or more agreements.
// The.NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Enums;

namespace Mocha.Storage.EntityFrameworkStorage.Trace;

public class Span
{
    public string TraceId { get; set; } = string.Empty;

    public string SpanId { get; set; } = string.Empty;

    public string SpanName { get; set; } = string.Empty;

    public string ParentSpanId { get; set; } = string.Empty;

    public string ServiceName { get; set; } = string.Empty;

    public long StartTime { get; set; }

    public long EndTime { get; set; }

    public double Duration { get; set; }

    public int StatusCode { get; set; }

    public string? StatusMessage { get; set; } = string.Empty;

    public SpanKind SpanKind { get; set; }

    public uint TraceFlags { get; set; }

    public string? TraceState { get; set; }= string.Empty;

    public IEnumerable<SpanLink> SpanLinks { get; set; } = Enumerable.Empty<SpanLink>();

    public IEnumerable<SpanAttribute> SpanAttributes { get; set; } =  Enumerable.Empty<SpanAttribute>();

    public IEnumerable<SpanEvent> SpanEvents { get; set; } =  Enumerable.Empty<SpanEvent>();
}
