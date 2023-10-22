namespace Mocha.Storage.EntityFrameworkStorage.Trace;
public class SpanEvent
{
    public string TraceId { get; private set; }= default!;

    public long TimeBucket { get; private set; }

    public string EventName { get; private set; }= default!;
}
