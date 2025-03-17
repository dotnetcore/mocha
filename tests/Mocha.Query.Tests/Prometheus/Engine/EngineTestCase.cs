using Mocha.Query.Prometheus.PromQL.Values;
using Xunit.Abstractions;

namespace Mocha.Query.Tests.Prometheus.Engine;

public class EngineTestCase : IXunitSerializable
{
    public required string Query { get; set; }

    public required IParseResult Result { get; set; }

    public long StartTimestampUnixSec { get; init; }

    public long EndTimestampUnixSec { get; init; }

    public TimeSpan Interval { get; init; }

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
