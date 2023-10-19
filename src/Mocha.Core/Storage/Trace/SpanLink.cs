namespace Mocha.Core.Model.Trace;

public class SpanLink
{
    public string TraceId { get; private set; }= default!;

    public string SpanId { get; private set; }= default!;

    public string LinkedSpanId { get; private set; }= default!;

    public string TraceState { get; private set; }= default!;

    public bool Flags { get; private set; }
}
