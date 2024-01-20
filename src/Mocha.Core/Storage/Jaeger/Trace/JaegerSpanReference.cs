namespace Mocha.Core.Storage.Jaeger.Trace
{
    public class JaegerSpanReference
    {
        public required string TraceID { get; init; }
        public required string SpanID { get; init; }
        public required string RefType { get; init; }
    }
}
