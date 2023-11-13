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
    private MemoryBufferQueue<int>? _memoryBufferQueue1;
    private MemoryBufferQueue<int>? _memoryBufferQueue2;

    [Params(4096, 8192)] public int MessageSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _blockingCollection = new BlockingCollection<int>();
        _memoryBufferQueue1 = new MemoryBufferQueue<int>(1);
        _memoryBufferQueue2 = new MemoryBufferQueue<int>(Environment.ProcessorCount);
        var producer1 = _memoryBufferQueue1.CreateProducer();
        var producer2 = _memoryBufferQueue2.CreateProducer();

        for (var i = 0; i < MessageSize; i++)
        {
            _blockingCollection.Add(i);
            producer1.ProduceAsync(i);
            producer2.ProduceAsync(i);
        }
    }

    [Benchmark]
    public void BlockingCollection_Concurrent_Consuming()
    {
        var countDownEvent = new CountdownEvent(10);
        for (var i = 0; i < Environment.ProcessorCount; i++)
        {
            _ = Task.Run(() =>
            {
                _blockingCollection!.Take();
                countDownEvent.Signal();
            });
        }

        countDownEvent.Wait();
    }

    [Benchmark]
    public void MemoryBufferQueue_Concurrent_Producing_Partition_1()
    {
        var countDownEvent = new CountdownEvent(MessageSize);
        for (var i = 0; i < Environment.ProcessorCount; i++)
        {
            _ = Task.Run(async () =>
            {
                var consumer =
                    _memoryBufferQueue1!.CreateConsumer(new BufferConsumerOptions
                    {
                        GroupName = "TestGroup", AutoCommit = true
                    });
                await foreach (var item in consumer.ConsumeAsync())
                {
                    countDownEvent.Signal();
                }
            });
        }

        countDownEvent.Wait();
    }

    [Benchmark]
    public void MemoryBufferQueue_Concurrent_Producing_Partition_ProcessorCount()
    {
        var countDownEvent = new CountdownEvent(MessageSize);
        for (var i = 0; i < Environment.ProcessorCount; i++)
        {
            _ = Task.Run(async () =>
            {
                var consumer =
                    _memoryBufferQueue2!.CreateConsumer(new BufferConsumerOptions
                    {
                        GroupName = "TestGroup", AutoCommit = true
                    });
                await foreach (var item in consumer.ConsumeAsync())
                {
                    countDownEvent.Signal();
                }
            });
        }

        countDownEvent.Wait();
    }
}
