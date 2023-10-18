namespace Mocha.Core.Model.Trace;

public class Span
{
    public string TraceId { get; private set; }

    public string SpanId { get; private set; }

    public string SpanName { get; private set; }

    public string ParentSpanId { get; private set; }

    public string StartTime { get; private set; }

    public string EndTime { get; private set; }

    public double Duration { get; private set; }

    public int StatusCode { get; private set; }

    public string StatusMessage { get; private set; }

    public int SpanKind { get; private set; }

    public string ServiceName { get; private set; }

    public bool TraceFlags { get; private set; }

    public bool TraceState { get; private set; }
}
