// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Query.Prometheus.PromQL.Contexts;

public record QueryTimeRange(
    long StartTimeUnixSec,
    long EndTimeUnixSec,
    TimeSpan Step)
{
    public IEnumerable<long> SplitToTimePoints()
    {
        for (var time = EndTimeUnixSec; time >= StartTimeUnixSec; time -= (long)Step.TotalSeconds)
        {
            yield return time;
        }
    }
}
