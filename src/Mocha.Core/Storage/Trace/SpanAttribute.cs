namespace Mocha.Core.Model.Trace;

public class SpanAttribute
{
    public string AttributeKey { get; private set; }= default!;

    public string AttributeValue { get; private set; }= default!;

    public long TimeBucket { get; private set; }

    public string TraceId { get; private set; }= default!;

    public string SpanId { get; private set; }= default!;
}
