// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Enums;

namespace Mocha.Storage.EntityFrameworkCore.Trace;

public class EFSpan
{
    public long Id { get; set; }

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

    public string? TraceState { get; set; }

    public ICollection<EFSpanLink> SpanLinks { get; set; } = new List<EFSpanLink>();

    public ICollection<EFSpanAttribute> SpanAttributes { get; set; } = new List<EFSpanAttribute>();

    public ICollection<EFSpanEvent> SpanEvents { get; set; } = new List<EFSpanEvent>();
}
