// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Query.Tests;

using System;
using System.Collections.Generic;
using Mocha.Core.Storage.Prometheus.Metrics;
using Mocha.Query.Prometheus.PromQL.Engine;
using Mocha.Query.Prometheus.PromQL.Values;
using Xunit;

public class MatrixEnumeratorTests
{
    [Fact]
    public void Enumerate_FirstCall_FillsWindow()
    {
        using var enumerator = new MatrixEnumerator(CreateSamples());
        var reused = new List<DoublePoint>();

        var result = enumerator.Enumerate(
            minTs: 0,
            maxTs: 20,
            reusedPoints: reused);

        Assert.Equal(new[] { 0L, 10L, 20L }, result.ConvertAll(p => p.TimestampUnixSec));
    }

    [Fact]
    public void Enumerate_OverlappingWindow_ReusesPoints()
    {
        using var enumerator = new MatrixEnumerator(CreateSamples());
        var reused = new List<DoublePoint>();

        // First window
        enumerator.Enumerate(0, 20, reused);

        // Overlapping window
        var result = enumerator.Enumerate(10, 30, reused);

        // keepFrom > 0 path
        Assert.Equal(new[] { 10L, 20L, 30L }, result.ConvertAll(p => p.TimestampUnixSec));
    }

    [Fact]
    public void Enumerate_NoOverlap_ClearsReusedPoints()
    {
        using var enumerator = new MatrixEnumerator(CreateSamples());
        var reused = new List<DoublePoint>();

        // First window
        enumerator.Enumerate(0, 10, reused);

        // Non-overlapping window
        var result = enumerator.Enumerate(30, 40, reused);

        // keepFrom == Count → Clear()
        Assert.Equal(new[] { 30L, 40L }, result.ConvertAll(p => p.TimestampUnixSec));
    }

    [Fact]
    public void Enumerate_WindowWithNoSamples_ReturnsEmpty()
    {
        using var enumerator = new MatrixEnumerator(CreateSamples());
        var reused = new List<DoublePoint>();

        var result = enumerator.Enumerate(
            minTs: 100,
            maxTs: 200,
            reusedPoints: reused);

        Assert.Empty(result);
    }

    [Fact]
    public void Enumerate_EnumeratorExhausted_DoesNotThrow()
    {
        using var enumerator = new MatrixEnumerator(CreateSamples());
        var reused = new List<DoublePoint>();

        // Consume everything
        enumerator.Enumerate(0, 100, reused);

        // Enumerator is exhausted
        var result = enumerator.Enumerate(0, 100, reused);

        Assert.NotNull(result);
    }

    [Fact]
    public void Enumerate_FutureSampleStopsConsumption()
    {
        using var enumerator = new MatrixEnumerator(CreateSamples());
        var reused = new List<DoublePoint>();

        // maxTs stops before 30
        var result = enumerator.Enumerate(0, 15, reused);

        // Should not consume sample at ts=20
        Assert.Equal(new[] { 0L, 10L }, result.ConvertAll(p => p.TimestampUnixSec));

        // Next window should still see 20
        var result2 = enumerator.Enumerate(0, 25, reused);

        Assert.Contains(result2, p => p.TimestampUnixSec == 20);
    }

    [Fact]
    public void Enumerate_Should_Not_Skip_Future_Sample()
    {
        var samples = new List<TimeSeriesSample>
        {
            new() { TimestampUnixSec = 0, Value = 0 },
            new() { TimestampUnixSec = 10, Value = 1 },
            new() { TimestampUnixSec = 20, Value = 2 }
        };

        using var enumerator = new MatrixEnumerator(samples);
        var reused = new List<DoublePoint>();

        // First window stops early
        var p1 = enumerator.Enumerate(minTs: 0, maxTs: 5, reused);
        Assert.Single(p1);
        Assert.Equal(0, p1[0].TimestampUnixSec);

        // Second window must still see ts=10
        var p2 = enumerator.Enumerate(minTs: 0, maxTs: 15, reused);
        Assert.Contains(p2, p => p.TimestampUnixSec == 10);
    }

    [Fact]
    public void Enumerate_InvalidWindow_Throws()
    {
        using var enumerator = new MatrixEnumerator(CreateSamples());
        var reused = new List<DoublePoint>();

        Assert.Throws<ArgumentException>(() =>
        {
            enumerator.Enumerate(
                minTs: 20,
                maxTs: 10,
                reusedPoints: reused);
        });
    }

    [Fact]
    public void Enumerate_NullReusedPoints_Throws()
    {
        using var enumerator = new MatrixEnumerator(CreateSamples());
        Assert.Throws<ArgumentNullException>(() =>
        {
            enumerator.Enumerate(
                minTs: 0,
                maxTs: 10,
                reusedPoints: null!);
        });
    }

    private static List<TimeSeriesSample> CreateSamples()
    {
        // ts: 0, 10, 20, 30, 40
        return
        [
            new TimeSeriesSample { TimestampUnixSec = 0, Value = 0 },
            new() { TimestampUnixSec = 10, Value = 1 },
            new() { TimestampUnixSec = 20, Value = 2 },
            new() { TimestampUnixSec = 30, Value = 3 },
            new() { TimestampUnixSec = 40, Value = 4 }
        ];
    }
}
