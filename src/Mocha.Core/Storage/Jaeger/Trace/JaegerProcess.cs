namespace Mocha.Core.Storage.Jaeger.Trace
{
    public class JaegerProcess
    {
        public required string ProcessID { get; init; }

        public required string ServiceName { get; init; }

        public required JaegerTag[] Tags { get; init; }
    }
}
