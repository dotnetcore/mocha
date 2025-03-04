using Mocha.Query.Prometheus.PromQL.Values;
using Xunit.Abstractions;

namespace Mocha.Query.Tests.Prometheus.Engine;

public class EngineTestCase : IXunitSerializable
{
    public string? Query { get; set; }

    public IParseResult? Result { get; set; }

    public long StartTimestampUnixSec { get; set; }

    public long EndTimestampUnixSec { get; set; }

    public TimeSpan Interval { get; set; }

    public void Deserialize(IXunitSerializationInfo info)
    {
        Query = info.GetValue<string>(nameof(Query));
        Result = info.GetValue<IParseResult>(nameof(Result));
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(Query), Query);
        info.AddValue(nameof(Result), Result);
    }
}
