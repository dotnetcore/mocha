// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Mocha.Core.Storage.Prometheus.Metrics;
using Mocha.Query.Prometheus.PromQL.Engine;
using Mocha.Query.Prometheus.PromQL.Values;

[MemoryDiagnoser]
public class MatrixEnumerationBenchmark
{
    [Params(1_000, 10_000)] public int SampleCount { get; set; }

    [Params(15)] public int SampleIntervalSeconds { get; set; }

    [Params(30, 60)] public int SelectorRangeSeconds { get; set; }

    private List<TimeSeriesSample> _samples = null!;
    private long _startTs;
    private long _endTs;

    [GlobalSetup]
    public void Setup()
    {
        _samples = new List<TimeSeriesSample>(SampleCount);

        long ts = 0;
        for (int i = 0; i < SampleCount; i++)
        {
            _samples.Add(new TimeSeriesSample { TimestampUnixSec = ts, Value = i });

            ts += SampleIntervalSeconds;
        }

        _startTs = _samples[0].TimestampUnixSec;
        _endTs = _samples[^1].TimestampUnixSec;
    }

    [Benchmark(Baseline = true)]
    public void LinqEveryStep()
    {
        for (var ts = _startTs; ts <= _endTs; ts += SelectorRangeSeconds)
        {
            var maxTs = ts;
            var minTs = maxTs - SelectorRangeSeconds;

            var points = _samples
                .Where(s => s.TimestampUnixSec >= minTs &&
                            s.TimestampUnixSec <= maxTs)
                .Select(s => new DoublePoint { TimestampUnixSec = s.TimestampUnixSec, Value = s.Value })
                .ToList();
        }
    }

    [Benchmark]
    public void EnumeratorSlidingWindow()
    {
        var reusePoints = new List<DoublePoint>();
        using var enumerator = new MatrixEnumerator(_samples);

        for (var ts = _startTs; ts <= _endTs; ts += SelectorRangeSeconds)
        {
            var maxTs = ts;
            var minTs = maxTs - SelectorRangeSeconds;

            enumerator.Enumerate(minTs, maxTs, reusePoints);
        }
    }
}
