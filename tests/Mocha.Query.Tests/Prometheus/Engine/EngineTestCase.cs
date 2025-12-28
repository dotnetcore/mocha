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
        StartTimestampUnixSec = info.GetValue<long>(nameof(StartTimestampUnixSec));
        EndTimestampUnixSec = info.GetValue<long>(nameof(EndTimestampUnixSec));
        Interval = info.GetValue<TimeSpan>(nameof(Interval));
        var resultValueType = info.GetValue<ResultValueType>(nameof(ResultValueType));
        switch (resultValueType)
        {
            case ResultValueType.Vector:
                Result = info.GetValue<VectorResult>(nameof(Result));
                break;
            case ResultValueType.Matrix:
                Result = info.GetValue<MatrixResult>(nameof(Result));
                break;
            case ResultValueType.Scalar:
                Result = info.GetValue<ScalarResult>(nameof(Result));
                break;
        }
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(Query), Query);
        info.AddValue(nameof(StartTimestampUnixSec), StartTimestampUnixSec);
        info.AddValue(nameof(EndTimestampUnixSec), EndTimestampUnixSec);
        info.AddValue(nameof(Interval), Interval);
        switch (Result)
        {
            case VectorResult vectorResult:
                info.AddValue(nameof(ResultValueType), ResultValueType.Vector);
                info.AddValue(nameof(Result), vectorResult);
                break;
            case MatrixResult matrixResult:
                info.AddValue(nameof(ResultValueType), ResultValueType.Matrix);
                info.AddValue(nameof(Result), matrixResult);
                break;
            case ScalarResult scalarResult:
                info.AddValue(nameof(ResultValueType), ResultValueType.Scalar);
                info.AddValue(nameof(Result), scalarResult);
                break;
        }
    }
}
