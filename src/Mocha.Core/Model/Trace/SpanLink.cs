namespace Mocha.Core.Model.Trace;

public class SpanLink
{
    public string TraceId { get; private set; }

    public string SpanId { get; private set; }

    public string LinkedSpanId { get; private set; }

    public string TraceState { get; private set; }

    public bool Flags { get; private set; }
}
