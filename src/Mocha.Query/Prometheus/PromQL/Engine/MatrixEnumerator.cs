// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Storage.Prometheus.Metrics;
using Mocha.Query.Prometheus.PromQL.Values;

namespace Mocha.Query.Prometheus.PromQL.Engine;

public class MatrixEnumerator(IEnumerable<TimeSeriesSample> samples) : IDisposable
{
    private readonly IEnumerator<TimeSeriesSample> _enumerator = samples.GetEnumerator();

    public List<DoublePoint> Enumerate(
        long minTs,
        long maxTs,
        List<DoublePoint> reusedPoints)
    {
        ArgumentNullException.ThrowIfNull(reusedPoints);

        if (minTs > maxTs)
        {
            throw new ArgumentException("minTs must be less than or equal to maxTs");
        }

        var keepFrom = 0;
        while (keepFrom < reusedPoints.Count && reusedPoints[keepFrom].TimestampUnixSec < minTs)
        {
            keepFrom++;
        }

        // If there is an overlap between previous and current ranges, keep the overlapping part.
        // If keepFrom is 0, all points are within the range, so keep them all.
        if (keepFrom > 0)
        {
            reusedPoints.RemoveRange(0, keepFrom);
        }
        else if (keepFrom == reusedPoints.Count)
        {
            // No overlap, clear all points.
            reusedPoints.Clear();
        }

        while (true)
        {
            // Current is uninitialized or has been fully consumed
            if (_enumerator.Current == null)
            {
                if (!_enumerator.MoveNext())
                {
                    break;
                }
            }

            var sample = _enumerator.Current;

            // Future data, leave it for the next step
            if (sample!.TimestampUnixSec > maxTs)
            {
                break;
            }

            // If the sample is within the range, add it to the points
            if (sample.TimestampUnixSec >= minTs)
            {
                reusedPoints.Add(new DoublePoint { TimestampUnixSec = sample.TimestampUnixSec, Value = sample.Value });
            }

            // Move to the next sample
            if (!_enumerator.MoveNext())
            {
                break;
            }
        }

        return reusedPoints;
    }

    public void Dispose()
    {
        _enumerator.Dispose();
    }
}
