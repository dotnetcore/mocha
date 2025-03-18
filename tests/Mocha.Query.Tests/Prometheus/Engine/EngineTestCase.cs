using Mocha.Query.Prometheus.PromQL.Values;
using Xunit.Abstractions;

namespace Mocha.Query.Tests.Prometheus.Engine;

// In order to have the test cases show up as individual tests,
// we need to implement IXunitSerializable.
public class EngineTestCase : IXunitSerializable
{
    public required string Query { get; set; }

    public required IParseResult Result { get; set; }

    public long StartTimestampUnixSec { get; set; }

    public long EndTimestampUnixSec { get; set; }

    public TimeSpan Interval { get; set; }

    public void Deserialize(IXunitSerializationInfo info)
    {
        Query = info.GetValue<string>(nameof(Query));
        Result = info.GetValue<IParseResult>(nameof(Result));
        StartTimestampUnixSec = info.GetValue<long>(nameof(StartTimestampUnixSec));
        EndTimestampUnixSec = info.GetValue<long>(nameof(EndTimestampUnixSec));
        Interval = info.GetValue<TimeSpan>(nameof(Interval));
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(Query), Query);
        info.AddValue(nameof(Result), Result);
        info.AddValue(nameof(StartTimestampUnixSec), StartTimestampUnixSec);
        info.AddValue(nameof(EndTimestampUnixSec), EndTimestampUnixSec);
        info.AddValue(nameof(Interval), Interval);
    }
}
