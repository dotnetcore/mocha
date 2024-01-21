// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;
using Mocha.Core.Buffer;
using Mocha.Core.Buffer.Memory;

namespace Mocha.Core.Benchmarks;

public class MemoryBufferQueueConsumeBenchmark
{
    private BlockingCollection<int>? _blockingCollection;
    private MemoryBufferQueue<int>? _memoryBufferQueue;
    private IEnumerable<IBufferConsumer<int>> _consumers = default!;

    [Params(4096, 8192)] public int MessageSize { get; set; }
    [Params(1, 10, 100, 1000)] public int BatchSize { get; set; }

    [IterationSetup]
    public void Setup()
    {
        _blockingCollection = new BlockingCollection<int>();
        _memoryBufferQueue = new MemoryBufferQueue<int>("test", Environment.ProcessorCount);
        var producer = _memoryBufferQueue.CreateProducer();

        for (var i = 0; i < MessageSize; i++)
        {
            _blockingCollection.Add(i);
            producer.ProduceAsync(i);
        }

        _consumers = _memoryBufferQueue!.CreateConsumers(
            new BufferConsumerOptions
            {
                GroupName = "TestGroup",
                TopicName = "test",
                AutoCommit = true,
                BatchSize = BatchSize,
            },
            Environment.ProcessorCount);
    }

    [Benchmark]
    public void BlockingCollection_Concurrent_Consuming()
    {
        var countDownEvent = new CountdownEvent(MessageSize);
        for (var i = 0; i < Environment.ProcessorCount; i++)
        {
            _ = Task.Run(() =>
            {
                while (true)
                {
                    countDownEvent.Signal();
                    _blockingCollection!.Take();
                }
            });
        }

        countDownEvent.Wait();
    }

    [Benchmark]
    public void MemoryBufferQueue_Concurrent_Consuming_Partition_ProcessorCount()
    {
        var countDownEvent = new CountdownEvent(MessageSize);

        foreach (var consumer in _consumers)
        {
            _ = Task.Run(async () =>
            {
                await foreach (var items in consumer.ConsumeAsync())
                {
                    countDownEvent.Signal(items.Count());
                }
            });
        }

        countDownEvent.Wait();
    }
}
