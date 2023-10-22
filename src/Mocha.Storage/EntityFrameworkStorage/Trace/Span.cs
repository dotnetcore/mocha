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

    public int SpanKind { get; private set; }

    public string ServiceName { get; set; } = default!;

    public bool TraceFlags { get; set; }

    public bool TraceState { get; set; }

    public ICollection<SpanLink> SpanLinks { get; set; } = new HashSet<SpanLink>();

    public ICollection<SpanAttribute> SpanAttributes { get; set; } = new HashSet<SpanAttribute>();

    public ICollection<SpanEvent> SpanEvents { get; set; } = new HashSet<SpanEvent>();
}
