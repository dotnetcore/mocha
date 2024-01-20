namespace Mocha.Core.Storage.Jaeger.Trace
{
    public class JaegerSpan
    {
        public required string TraceID { get; init; }

        public required string SpanID { get; init; }

        public required string OperationName { get; init; }

        public uint Flags { get; init; }

        public ulong StartTime { get; init; }

        public ulong Duration { get; init; }

        public required string ProcessID { get; init; }

        public required JaegerSpanReference[] References { get; init; }

        public required JaegerTag[] Tags { get; init; }

        public required JaegerSpanLog[] Logs { get; init; }
    }
}
