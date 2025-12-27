// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Options;
using Mocha.Core.Models.Metrics;
using Mocha.Core.Storage;
using Mocha.Core.Storage.Prometheus;
using Mocha.Core.Storage.Prometheus.Metrics;
using Mocha.Storage.LiteDB;
using Mocha.Storage.LiteDB.Metrics;
using Mocha.Storage.LiteDB.Metrics.Readers.Prometheus;
using Mocha.Storage.LiteDB.Metrics.Writers;

namespace Mocha.Storage.Benchmarks;

[MemoryDiagnoser]
public class PrometheusMetricsReaderBenchmark
{
    private IPrometheusMetricsReader? _liteDBReader;
    private int[]? _randomRoutes;

    private const string DatabasePath = "benchmark_metrics";

    [GlobalSetup]
    public void Setup()
    {
        var options = Options.Create(new LiteDBMetricsOptions { DatabasePath = DatabasePath });

        var collectionAccessor = new LiteDBMetricsCollectionAccessor(options);

        _liteDBReader = new LiteDBPrometheusMetricsReader(collectionAccessor);
        var writer = new LiteDBMetricsWriter(collectionAccessor);
        WriteSampleDataAsync(writer).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        var dbPath = Path.Combine(DatabasePath, LiteDBConstants.MetricsDatabaseFileName);
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }

    [IterationSetup]
    public void IterationSetup()
    {
        var rand = new Random();
        _randomRoutes = Enumerable.Range(1, 10000).OrderBy(x => rand.Next()).Take(100).ToArray();
    }

    [Benchmark]
    public async Task LiteDB()
    {
        foreach (var route in _randomRoutes)
        {
            var query = new TimeSeriesQueryParameters
            {
                LabelMatchers =
                [
                    new LabelMatcher(
                        Name: "http_route",
                        Type: LabelMatcherType.Equal,
                        Value: $"/api/resource/{route}")
                ],
                StartTimestampUnixSec = DateTimeOffset.UtcNow.AddHours(-1)
                    .ToUnixTimeSeconds(),
                EndTimestampUnixSec = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Limit = 1000,
                Interval = TimeSpan.Zero
            };

            var result = await _liteDBReader.GetTimeSeriesAsync(query, CancellationToken.None);
        }
    }

    private async Task WriteSampleDataAsync(ITelemetryDataWriter<MochaMetric> writer)
    {
        // write 1,000,000 metrics with high cardinality
        var metrics = new List<MochaMetric>();
        for (var i = 1; i <= 10_000; i++)
        {
            for (var j = 1; j <= 100; j++)
            {
                metrics.Add(new MochaMetric
                {
                    Name = "http_requests_total",
                    Labels = new Labels { { "method", "GET" }, { "http_route", $"/api/resource/{i}" } },
                    Value = i * j,
                    TimestampUnixNano = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000,
                    Description = string.Empty,
                    Unit = "requests"
                });
            }
        }

        const int batchSize = 1000;
        for (var k = 0; k < metrics.Count; k += batchSize)
        {
            var batch = metrics.Skip(k).Take(batchSize);
            await writer.WriteAsync(batch);
        }
    }
}
