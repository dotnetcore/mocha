// Licensed to the .NET Core Community under one or more agreements.
// The.NET Core Community licenses this file to you under the MIT license.

using System.Reflection;
using Mocha.Core.Buffer;
using Mocha.Core.Buffer.Memory;

namespace Mocha.Core.Tests.Buffer.Memory;

public class MemoryBufferQueueTests
{
    public MemoryBufferQueueTests()
    {
        // Avoid deadlock when testing,
        // xunit may set the SynchronizationContext to a single-threaded context
        SynchronizationContext.SetSynchronizationContext(null);
        // Avoid the impact of the default segment length on the test
        MemoryBufferPartition<int>.SegmentLength = 1024;
    }

    [Fact]
    public async Task Produce_And_Consume()
    {
        var queue = new MemoryBufferQueue<int>(1);
        var producer = queue.CreateProducer();
        var consumer = queue.CreateConsumer(new BufferConsumerOptions { GroupName = "TestGroup", AutoCommit = false });

        var expectedValues = new int[10];
        for (var i = 0; i < 10; i++)
        {
            await producer.ProduceAsync(i);
            expectedValues[i] = i;
        }

        var index = 0;
        await foreach (var item in consumer.ConsumeAsync())
        {
            Assert.Equal(expectedValues[index++], item);
            var valueTask = consumer.CommitAsync();
            if (!valueTask.IsCompletedSuccessfully)
            {
                await valueTask.AsTask();
            }

            if (index == 10)
            {
                break;
            }
        }
    }

    [Fact]
    public async Task Produce_And_Consume_AutoCommit()
    {
        var queue = new MemoryBufferQueue<int>(1);
        var producer = queue.CreateProducer();
        var consumer = queue.CreateConsumer(new BufferConsumerOptions { GroupName = "TestGroup", AutoCommit = true });

        var expectedValues = new int[10];
        for (var i = 0; i < 10; i++)
        {
            await producer.ProduceAsync(i);
            expectedValues[i] = i;
        }

        var index = 0;
        await foreach (var item in consumer.ConsumeAsync())
        {
            Assert.Equal(expectedValues[index++], item);
            if (index == 10)
            {
                break;
            }
        }
    }

    [Fact]
    public async Task Produce_And_Consume_With_Multiple_Partitions()
    {
        var queue = new MemoryBufferQueue<int>(2);
        var producer = queue.CreateProducer();
        var consumer = queue.CreateConsumer(new BufferConsumerOptions { GroupName = "TestGroup", AutoCommit = false });

        var expectedValues = new int[10];
        for (var i = 0; i < 10; i++)
        {
            await producer.ProduceAsync(i);
            expectedValues[i] = i;
        }

        var index = 0;
        await foreach (var item in consumer.ConsumeAsync())
        {
            Assert.Equal(expectedValues[index++], item);
            var valueTask = consumer.CommitAsync();
            if (!valueTask.IsCompletedSuccessfully)
            {
                await valueTask.AsTask();
            }

            if (index == 10)
            {
                break;
            }
        }
    }

    [Fact]
    public async Task Produce_And_Consume_With_Multiple_Consumers()
    {
        var queue = new MemoryBufferQueue<int>(2);
        var producer = queue.CreateProducer();
        var consumers = queue
            .CreateConsumers(new BufferConsumerOptions { GroupName = "TestGroup", AutoCommit = false }, 2).ToList();
        var consumer1 = consumers[0];
        var consumer2 = consumers[1];

        await producer.ProduceAsync(1);
        await producer.ProduceAsync(2);

        await foreach (var item in consumer1.ConsumeAsync())
        {
            Assert.Equal(1, item);
            break;
        }

        await foreach (var item in consumer2.ConsumeAsync())
        {
            Assert.Equal(2, item);
            break;
        }
    }

    [Fact]
    public async Task Offset_Will_Not_Change_If_Consumer_Not_Commit()
    {
        var queue = new MemoryBufferQueue<int>(1);
        var producer = queue.CreateProducer();
        var consumer = queue.CreateConsumer(new BufferConsumerOptions { GroupName = "TestGroup", AutoCommit = false });

        await producer.ProduceAsync(1);
        await producer.ProduceAsync(2);

        await foreach (var item in consumer.ConsumeAsync())
        {
            Assert.Equal(1, item);
            break;
        }

        await foreach (var item in consumer.ConsumeAsync())
        {
            Assert.Equal(1, item);
            break;
        }

        var valueTask = consumer.CommitAsync();
        if (!valueTask.IsCompletedSuccessfully)
        {
            await valueTask.AsTask();
        }

        await foreach (var item in consumer.ConsumeAsync())
        {
            Assert.Equal(2, item);
            break;
        }
    }

    [Fact]
    public async Task Consumer_Will_Wait_Until_Produce()
    {
        var queue = new MemoryBufferQueue<int>(1);
        var producer = queue.CreateProducer();
        var consumer = queue.CreateConsumer(new BufferConsumerOptions { GroupName = "TestGroup", AutoCommit = false });

        var task = Task.Run(async () =>
        {
            await foreach (var item in consumer.ConsumeAsync())
            {
                Assert.Equal(1, item);
                break;
            }
        });

        await Task.Delay(100);

        await producer.ProduceAsync(1);

        await task;
    }

    [Fact]
    public async Task Retry_Consumption_If_No_Committed_Offset()
    {
        var queue = new MemoryBufferQueue<int>(1);
        var producer = queue.CreateProducer();
        var consumer = queue.CreateConsumer(new BufferConsumerOptions { GroupName = "TestGroup", AutoCommit = false });

        await producer.ProduceAsync(1);
        await producer.ProduceAsync(2);

        var index = 0;
        await foreach (var item in consumer.ConsumeAsync())
        {
            if (index < 9)
            {
                Assert.Equal(1, item);
            }
            else if (index == 9)
            {
                Assert.Equal(2, item);
                break;
            }

            if (index == 8)
            {
                var valueTask = consumer.CommitAsync();
                if (!valueTask.IsCompletedSuccessfully)
                {
                    await valueTask.AsTask();
                }
            }

            index++;
        }
    }

    [Fact]
    public void Equal_Distribution_Load_Balancing_Strategy_For_Consumers()
    {
        var queue = new MemoryBufferQueue<int>(18);

        var assignedPartitionsFieldInfo = typeof(MemoryBufferConsumer<int>)
            .GetField("_assignedPartitions", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var group1Consumers =
            queue.CreateConsumers(new BufferConsumerOptions { GroupName = "TestGroup1", AutoCommit = false }, 3)
                .ToList();

        var group2Consumers = queue
            .CreateConsumers(new BufferConsumerOptions { GroupName = "TestGroup2", AutoCommit = false }, 4)
            .ToList();

        var group3Consumers = queue
            .CreateConsumers(new BufferConsumerOptions { GroupName = "TestGroup3", AutoCommit = false }, 5)
            .ToList();

        var group4Consumers = queue
            .CreateConsumers(new BufferConsumerOptions { GroupName = "TestGroup4", AutoCommit = false }, 16)
            .ToList();

        for (var i = 0; i < 3; i++)
        {
            var partitions =
                (MemoryBufferPartition<int>[])assignedPartitionsFieldInfo.GetValue(group1Consumers[i])!;
            Assert.Equal(6, partitions.Length);
        }

        for (var i = 0; i < 4; i++)
        {
            var partitions =
                (MemoryBufferPartition<int>[])assignedPartitionsFieldInfo.GetValue(group2Consumers[i])!;
            Assert.Equal(i < 2 ? 5 : 4, partitions.Length);
        }

        for (var i = 0; i < 5; i++)
        {
            var partitions =
                (MemoryBufferPartition<int>[])assignedPartitionsFieldInfo.GetValue(group3Consumers[i])!;
            Assert.Equal(i < 3 ? 4 : 3, partitions.Length);
        }

        for (var i = 0; i < 16; i++)
        {
            var partitions =
                (MemoryBufferPartition<int>[])assignedPartitionsFieldInfo.GetValue(group4Consumers[i])!;
            Assert.Equal(i < 2 ? 2 : 1, partitions.Length);
        }
    }

    [Fact]
    public void Concurrent_Producer_Single_Partition()
    {
        var messageSize = MemoryBufferPartition<int>.SegmentLength * 4;

        var queue = new MemoryBufferQueue<int>(1);

        var countDownEvent = new CountdownEvent(messageSize);
        var consumer = queue.CreateConsumer(new BufferConsumerOptions { GroupName = "TestGroup", AutoCommit = true });
        _ = Task.Run(async () =>
        {
            await foreach (var item in consumer.ConsumeAsync())
            {
                if (countDownEvent.Signal())
                {
                    break;
                }
            }
        });

        var producer = queue.CreateProducer();
        var chunkSize = (int)Math.Ceiling(messageSize * 1.0d / Environment.ProcessorCount);
        var chunks = Enumerable.Range(0, messageSize).Chunk(chunkSize);
        foreach (var chunk in chunks)
        {
            _ = Task.Run(async () =>
            {
                foreach (var item in chunk)
                {
                    var valueTask = producer.ProduceAsync(item);
                    if (!valueTask.IsCompletedSuccessfully)
                    {
                        await valueTask.AsTask();
                    }
                }
            });
        }

        countDownEvent.Wait();
    }

    [Fact]
    public void Concurrent_Producer_Multiple_Partition()
    {
        var messageSize = MemoryBufferPartition<int>.SegmentLength * 4;

        var queue = new MemoryBufferQueue<int>(Environment.ProcessorCount);

        var consumer = queue.CreateConsumer(new BufferConsumerOptions { GroupName = "TestGroup", AutoCommit = true });
        var countDownEvent = new CountdownEvent(messageSize);
        _ = Task.Run(async () =>
        {
            await foreach (var item in consumer.ConsumeAsync())
            {
                if (countDownEvent.Signal())
                {
                    break;
                }
            }
        });

        var producer = queue.CreateProducer();
        var chunkSize = (int)Math.Ceiling(messageSize * 1.0d / Environment.ProcessorCount);
        var chunks = Enumerable.Range(0, messageSize).Chunk(chunkSize);
        foreach (var chunk in chunks)
        {
            _ = Task.Run(async () =>
            {
                foreach (var item in chunk)
                {
                    var valueTask = producer.ProduceAsync(item);
                    if (!valueTask.IsCompletedSuccessfully)
                    {
                        await valueTask.AsTask();
                    }
                }
            });
        }

        countDownEvent.Wait();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Concurrent_Consumer_Multiple_Groups(int groupNumber)
    {
        var messageSize = MemoryBufferPartition<int>.SegmentLength * 4;
        var partitionNumber = Environment.ProcessorCount * 2;
        var consumerNumberPerGroup = Environment.ProcessorCount;

        var queue = new MemoryBufferQueue<int>(partitionNumber);

        var countdownEvent = new CountdownEvent(messageSize * groupNumber);

        for (var groupIndex = 0; groupIndex < groupNumber; groupIndex++)
        {
            var consumers = queue
                .CreateConsumers(
                    new BufferConsumerOptions { GroupName = "TestGroup" + (groupIndex + 1), AutoCommit = true },
                    consumerNumberPerGroup)
                .ToList();

            foreach (var consumer in consumers)
            {
                _ = Task.Run(async () =>
                {
                    await foreach (var item in consumer.ConsumeAsync())
                    {
                        if (countdownEvent.Signal())
                        {
                            break;
                        }
                    }
                });
            }
        }

        var producer = queue.CreateProducer();

        _ = Task.Run(async () =>
        {
            for (var i = 0; i < messageSize; i++)
            {
                var valueTask = producer.ProduceAsync(i);
                if (!valueTask.IsCompletedSuccessfully)
                {
                    await valueTask.AsTask();
                }
            }
        });

        countdownEvent.Wait();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Concurrent_Producer_And_Concurrent_Consumer_Multiple_Groups(int groupNumber)
    {
        var messageSize = MemoryBufferPartition<int>.SegmentLength * 4;
        var partitionNumber = Environment.ProcessorCount * 2;
        var consumerNumberPerGroup = Environment.ProcessorCount;

        var queue = new MemoryBufferQueue<int>(partitionNumber);

        var countdownEvent = new CountdownEvent(messageSize * groupNumber);

        for (var groupIndex = 0; groupIndex < groupNumber; groupIndex++)
        {
            var consumers = queue
                .CreateConsumers(
                    new BufferConsumerOptions { GroupName = "TestGroup" + (groupIndex + 1), AutoCommit = true },
                    consumerNumberPerGroup)
                .ToList();

            foreach (var consumer in consumers)
            {
                _ = Task.Run(async () =>
                {
                    await foreach (var item in consumer.ConsumeAsync())
                    {
                        if (countdownEvent.Signal())
                        {
                            break;
                        }
                    }
                });
            }
        }

        var producer = queue.CreateProducer();
        var chunkSize = (int)Math.Ceiling(messageSize * 1.0d / partitionNumber);
        var chunks = Enumerable.Range(0, messageSize).Chunk(chunkSize);

        foreach (var chunk in chunks)
        {
            _ = Task.Run(async () =>
            {
                foreach (var item in chunk)
                {
                    var valueTask = producer.ProduceAsync(item);
                    if (!valueTask.IsCompletedSuccessfully)
                    {
                        await valueTask.AsTask();
                    }
                }
            });
        }

        countdownEvent.Wait();
    }
}
