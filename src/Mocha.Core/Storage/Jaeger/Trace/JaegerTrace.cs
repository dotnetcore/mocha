namespace Mocha.Core.Storage.Jaeger.Trace
{
    public class JaegerTrace
    {
        public required string TraceID { get; set; }

        public required Dictionary<string, JaegerProcess> Processes { get; set; }

        public required JaegerSpan[] Spans { get; set; }
    }
}
