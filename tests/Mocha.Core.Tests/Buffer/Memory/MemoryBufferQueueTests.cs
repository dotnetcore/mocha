// Licensed to the .NET Core Community under one or more agreements.
// The.NET Core Community licenses this file to you under the MIT license.

using System.Reflection;
using Mocha.Core.Buffer;
using Mocha.Core.Buffer.Memory;

namespace Mocha.Core.Tests.Buffer.Memory;

public class MemoryBufferQueueTests
{
    private static int MemoryBufferPartitionSegmentLength => new MemoryBufferPartition<int>(0)._segmentLength;

    [Fact]
    public async Task Produce_And_Consume()
    {
        var queue = new MemoryBufferQueue<int>("test", 1);
        var producer = queue.CreateProducer();
        var consumer = queue.CreateConsumer(new BufferConsumerOptions
        {
            TopicName = "test",
            GroupName = "TestGroup",
            AutoCommit = false,
            BatchSize = 2
        });

        var expectedValues = new int[10];
        for (var i = 0; i < 10; i++)
        {
            await producer.ProduceAsync(i);
            expectedValues[i] = i;
        }

        var index = 0;
        await foreach (var items in consumer.ConsumeAsync())
        {
            foreach (var item in items)
            {
                Assert.Equal(expectedValues[index++], item);
            }

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
        var queue = new MemoryBufferQueue<int>("test", 1);
        var producer = queue.CreateProducer();
        var consumer = queue.CreateConsumer(
            new BufferConsumerOptions
            {
                TopicName = "test",
                GroupName = "TestGroup",
                AutoCommit = true,
                BatchSize = 2
            });

        var expectedValues = new int[10];
        for (var i = 0; i < 10; i++)
        {
            await producer.ProduceAsync(i);
            expectedValues[i] = i;
        }

        var index = 0;
        await foreach (var items in consumer.ConsumeAsync())
        {
            foreach (var item in items)
            {
                Assert.Equal(expectedValues[index++], item);
            }

            if (index == 10)
            {
                break;
            }
        }
    }

    [Fact]
    public async Task Produce_And_Consume_With_Multiple_Partitions()
    {
        var queue = new MemoryBufferQueue<int>("test", 2);
        var producer = queue.CreateProducer();
        var consumer = queue.CreateConsumer(
            new BufferConsumerOptions
            {
                TopicName = "test",
                GroupName = "TestGroup",
                AutoCommit = false,
                BatchSize = 2
            });

        var expectedValues = new int[10];
        for (var i = 0; i < 10; i++)
        {
            await producer.ProduceAsync(i);
            expectedValues[i] = i;
        }

        var consumedValues = new List<int>();
        await foreach (var items in consumer.ConsumeAsync())
        {
            consumedValues.AddRange(items);

            if (consumedValues.Count == 10)
            {
                break;
            }

            var valueTask = consumer.CommitAsync();
            if (!valueTask.IsCompletedSuccessfully)
            {
                await valueTask.AsTask();
            }
        }

        Assert.Equal(expectedValues, consumedValues.OrderBy(x => x));
    }

    [Fact]
    public async Task Produce_And_Consume_With_Multiple_Consumers()
    {
        var queue = new MemoryBufferQueue<int>("test", 2);
        var producer = queue.CreateProducer();
        var consumers = queue
            .CreateConsumers(
                new BufferConsumerOptions
                {
                    TopicName = "test",
                    GroupName = "TestGroup",
                    AutoCommit = false,
                    BatchSize = 6
                },
                2).ToList();
        var consumer1 = consumers[0];
        var consumer2 = consumers[1];

        for (var i = 0; i < 10; i++)
        {
            await producer.ProduceAsync(i);
        }

        await foreach (var items in consumer1.ConsumeAsync())
        {
            Assert.Equal(new[] { 0, 2, 4, 6, 8 }, items);
            break;
        }

        await foreach (var items in consumer2.ConsumeAsync())
        {
            Assert.Equal(new[] { 1, 3, 5, 7, 9 }, items);
            break;
        }
    }

    [Fact]
    public void Throw_If_Wrong_Consumer_Number()
    {
        var queue = new MemoryBufferQueue<int>("test", 2);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            queue.CreateConsumers(
                new BufferConsumerOptions
                {
                    TopicName = "test",
                    GroupName = "TestGroup",
                    AutoCommit = false,
                    BatchSize = 6
                },
                3).ToList());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            queue.CreateConsumers(
                new BufferConsumerOptions
                {
                    TopicName = "test",
                    GroupName = "TestGroup",
                    AutoCommit = false,
                    BatchSize = 6
                },
                0).ToList());
    }

    [Fact]
    public async Task Offset_Will_Not_Change_If_Consumer_Not_Commit()
    {
        var queue = new MemoryBufferQueue<int>("test", 1);
        var producer = queue.CreateProducer();
        var consumer = queue.CreateConsumer(
            new BufferConsumerOptions
            {
                TopicName = "test",
                GroupName = "TestGroup",
                AutoCommit = false,
                BatchSize = 7
            });

        for (var i = 0; i < 10; i++)
        {
            await producer.ProduceAsync(i);
        }

        await foreach (var items in consumer.ConsumeAsync())
        {
            Assert.Equal(new[] { 0, 1, 2, 3, 4, 5, 6 }, items);
            break;
        }

        await foreach (var items in consumer.ConsumeAsync())
        {
            Assert.Equal(new[] { 0, 1, 2, 3, 4, 5, 6 }, items);
            break;
        }

        var valueTask = consumer.CommitAsync();
        if (!valueTask.IsCompletedSuccessfully)
        {
            await valueTask.AsTask();
        }

        await foreach (var items in consumer.ConsumeAsync())
        {
            Assert.Equal(new[] { 7, 8, 9 }, items);
            break;
        }
    }

    [Fact]
    public async Task Consumer_Will_Wait_Until_Produce()
    {
        var queue = new MemoryBufferQueue<int>("test", 1);
        var producer = queue.CreateProducer();
        var consumer =
            queue.CreateConsumer(new BufferConsumerOptions
            {
                TopicName = "test",
                GroupName = "TestGroup",
                AutoCommit = false
            });

        var task = Task.Run(async () =>
        {
            await foreach (var items in consumer.ConsumeAsync())
            {
                Assert.Equal(1, items.Single());
                break;
            }
        });

        await Task.Delay(100);

        await producer.ProduceAsync(1);

        await task;
    }

    [Fact]
    public void Equal_Distribution_Load_Balancing_Strategy_For_Consumers()
    {
        var queue = new MemoryBufferQueue<int>("test", 18);

        var assignedPartitionsFieldInfo = typeof(MemoryBufferConsumer<int>)
            .GetField("_assignedPartitions", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var group1Consumers =
            queue.CreateConsumers(
                    new BufferConsumerOptions { TopicName = "test", GroupName = "TestGroup1", AutoCommit = false }, 3)
                .ToList();

        var group2Consumers = queue
            .CreateConsumers(
                new BufferConsumerOptions { TopicName = "test", GroupName = "TestGroup2", AutoCommit = false }, 4)
            .ToList();

        var group3Consumers = queue
            .CreateConsumers(
                new BufferConsumerOptions { TopicName = "test", GroupName = "TestGroup3", AutoCommit = false }, 5)
            .ToList();

        var group4Consumers = queue
            .CreateConsumers(
                new BufferConsumerOptions { TopicName = "test", GroupName = "TestGroup4", AutoCommit = false }, 16)
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
        var messageSize = MemoryBufferPartitionSegmentLength * 4;

        var queue = new MemoryBufferQueue<int>("test", 1);

        var countDownEvent = new CountdownEvent(messageSize);
        var consumer =
            queue.CreateConsumer(new BufferConsumerOptions
            {
                TopicName = "test",
                GroupName = "TestGroup",
                AutoCommit = true
            });
        _ = Task.Run(async () =>
        {
            await foreach (var items in consumer.ConsumeAsync())
            {
                if (countDownEvent.Signal(items.Count()))
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
        var messageSize = MemoryBufferPartitionSegmentLength * 4;

        var queue = new MemoryBufferQueue<int>("test", Environment.ProcessorCount);

        var consumer =
            queue.CreateConsumer(new BufferConsumerOptions
            {
                TopicName = "test",
                GroupName = "TestGroup",
                AutoCommit = true
            });
        var countDownEvent = new CountdownEvent(messageSize);
        _ = Task.Run(async () =>
        {
            await foreach (var items in consumer.ConsumeAsync())
            {
                if (countDownEvent.Signal(items.Count()))
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
    [InlineData(1, 1)]
    [InlineData(1, 10)]
    [InlineData(1, 100)]
    [InlineData(1, 1000)]
    [InlineData(1, 10000)]
    [InlineData(2, 1)]
    [InlineData(2, 10)]
    [InlineData(2, 100)]
    [InlineData(2, 1000)]
    [InlineData(2, 10000)]
    [InlineData(3, 1)]
    [InlineData(3, 10)]
    [InlineData(3, 100)]
    [InlineData(3, 1000)]
    [InlineData(3, 10000)]
    public void Concurrent_Consumer_Multiple_Groups(int groupNumber, int batchSize)
    {
        var messageSize = MemoryBufferPartitionSegmentLength * 4;
        var partitionNumber = Environment.ProcessorCount * 2;
        var consumerNumberPerGroup = Environment.ProcessorCount;

        var queue = new MemoryBufferQueue<int>("test", partitionNumber);

        var countdownEvent = new CountdownEvent(messageSize * groupNumber);

        for (var groupIndex = 0; groupIndex < groupNumber; groupIndex++)
        {
            var consumers = queue
                .CreateConsumers(
                    new BufferConsumerOptions
                    {
                        TopicName = "test",
                        GroupName = "TestGroup" + (groupIndex + 1),
                        AutoCommit = true,
                        BatchSize = batchSize
                    },
                    consumerNumberPerGroup)
                .ToList();

            foreach (var consumer in consumers)
            {
                _ = Task.Run(async () =>
                {
                    await foreach (var items in consumer.ConsumeAsync())
                    {
                        if (countdownEvent.Signal(items.Count()))
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
        var messageSize = MemoryBufferPartitionSegmentLength * 4;
        var partitionNumber = Environment.ProcessorCount * 2;
        var consumerNumberPerGroup = Environment.ProcessorCount;

        var queue = new MemoryBufferQueue<int>("test", partitionNumber);

        var countdownEvent = new CountdownEvent(messageSize * groupNumber);

        for (var groupIndex = 0; groupIndex < groupNumber; groupIndex++)
        {
            var consumers = queue
                .CreateConsumers(
                    new BufferConsumerOptions
                    {
                        TopicName = "test",
                        GroupName = "TestGroup" + (groupIndex + 1),
                        AutoCommit = true
                    },
                    consumerNumberPerGroup)
                .ToList();

            foreach (var consumer in consumers)
            {
                _ = Task.Run(async () =>
                {
                    await foreach (var items in consumer.ConsumeAsync())
                    {
                        if (countdownEvent.Signal(items.Count()))
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
