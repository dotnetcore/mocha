namespace Mocha.Core.Model.Trace;

public class Span
{
    public string TraceId { get; private set; } = default!;

    public string SpanId { get; private set; }= default!;

    public string SpanName { get; private set; }= default!;

    public string ParentSpanId { get; private set; }= default!;

    public string StartTime { get; private set; }= default!;

    public string EndTime { get; private set; }= default!;

    public double Duration { get; private set; }

    public int StatusCode { get; private set; }

    public string StatusMessage { get; private set; }= default!;

    public int SpanKind { get; private set; }

    public string ServiceName { get; private set; }= default!;

    public bool TraceFlags { get; private set; }

    public bool TraceState { get; private set; }



}
