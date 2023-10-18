namespace Mocha.Core.Model.Trace;

public class SpanAttribute
{
    public string AttributeKey { get; private set; }

    public string AttributeValue { get; private set; }

    public long TimeBucket { get; private set; }

    public string TraceId { get; private set; }

    public string SpanId { get; private set; }
}
