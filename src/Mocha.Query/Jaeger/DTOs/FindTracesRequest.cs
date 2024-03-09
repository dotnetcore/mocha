namespace Mocha.Query.Jaeger.DTOs
{
    public class FindTracesRequest
    {
        public string[]? TraceID { get; set; }

        public string? Service { get; set; }

        public string? Operation { get; set; }

        public string? Tags { get; set; }

        public string? LookBack { get; set; }

        public ulong? Start { get; set; }

        public ulong? End { get; set; }

        public string? MinDuration { get; set; }

        public string? MaxDuration { get; set; }

        public int Limit { get; set; }
    }
}
