// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;
using Mocha.Core.Buffer.Memory;

namespace Mocha.Core.Benchmarks;

public class MemoryBufferQueueProduceBenchmark
{
    private int[][] _chunks = default!;

    [Params(4096, 8192)] public int MessageSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var chunkSize = (int)Math.Ceiling(MessageSize * 1.0d / Environment.ProcessorCount);
        _chunks = Enumerable.Range(0, MessageSize).Chunk(chunkSize).ToArray();
    }

    [Benchmark(Baseline = true)]
    public async Task BlockingCollection_Concurrent_Producing()
    {
        var queue = new BlockingCollection<int>();
        var tasks = _chunks.Select(chunk => Task.Run(() =>
        {
            foreach (var item in chunk)
            {
                queue.Add(item);
            }
        })).ToArray();

        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task MemoryBufferQueue_Concurrent_Producing_Partition_1()
    {
        var queue = new MemoryBufferQueue<int>("test", 1);
        var producer = queue.CreateProducer();
        var tasks = _chunks.Select(chunk => Task.Run(async () =>
        {
            foreach (var item in chunk)
            {
                var valueTask = producer.ProduceAsync(item);
                if (!valueTask.IsCompletedSuccessfully)
                {
                    await valueTask.AsTask();
                }
            }
        })).ToArray();

        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task MemoryBufferQueue_Concurrent_Producing_Partition_ProcessorCount()
    {
        var queue = new MemoryBufferQueue<int>("test", Environment.ProcessorCount);
        var producer = queue.CreateProducer();
        var tasks = _chunks.Select(chunk => Task.Run(async () =>
        {
            foreach (var item in chunk)
            {
                var valueTask = producer.ProduceAsync(item);
                if (!valueTask.IsCompletedSuccessfully)
                {
                    await valueTask.AsTask();
                }
            }
        })).ToArray();

        await Task.WhenAll(tasks);
    }
}
