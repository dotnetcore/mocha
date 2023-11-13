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
        var consumer1 = queue.CreateConsumer(new BufferConsumerOptions { GroupName = "TestGroup", AutoCommit = false });
        var consumer2 = queue.CreateConsumer(new BufferConsumerOptions { GroupName = "TestGroup", AutoCommit = false });

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

        var group1Customers = new IBufferConsumer<int>[4];
        for (var i = 0; i < 3; i++)
        {
            group1Customers[i] =
                queue.CreateConsumer(new BufferConsumerOptions { GroupName = "TestGroup1", AutoCommit = false });
        }

        var group2Customers = new IBufferConsumer<int>[4];
        for (var i = 0; i < 4; i++)
        {
            group2Customers[i] =
                queue.CreateConsumer(new BufferConsumerOptions { GroupName = "TestGroup2", AutoCommit = false });
        }

        var group3Customers = new IBufferConsumer<int>[5];
        for (var i = 0; i < 5; i++)
        {
            group3Customers[i] =
                queue.CreateConsumer(new BufferConsumerOptions { GroupName = "TestGroup3", AutoCommit = false });
        }

        var group4Customers = new IBufferConsumer<int>[16];
        for (var i = 0; i < 16; i++)
        {
            group4Customers[i] =
                queue.CreateConsumer(new BufferConsumerOptions { GroupName = "TestGroup4", AutoCommit = false });
        }

        for (var i = 0; i < 3; i++)
        {
            var partitions =
                (MemoryBufferPartition<int>[])assignedPartitionsFieldInfo.GetValue(group1Customers[i])!;
            Assert.Equal(6, partitions.Length);
        }

        for (var i = 0; i < 4; i++)
        {
            var partitions =
                (MemoryBufferPartition<int>[])assignedPartitionsFieldInfo.GetValue(group2Customers[i])!;
            Assert.Equal(i < 2 ? 5 : 4, partitions.Length);
        }

        for (var i = 0; i < 5; i++)
        {
            var partitions =
                (MemoryBufferPartition<int>[])assignedPartitionsFieldInfo.GetValue(group3Customers[i])!;
            Assert.Equal(i < 3 ? 4 : 3, partitions.Length);
        }

        for (var i = 0; i < 16; i++)
        {
            var partitions =
                (MemoryBufferPartition<int>[])assignedPartitionsFieldInfo.GetValue(group4Customers[i])!;
            Assert.Equal(i < 2 ? 2 : 1, partitions.Length);
        }
    }

    [Fact]
    public void Rebalance_If_New_Customer_Created()
    {
        var queue = new MemoryBufferQueue<int>(9);

        var assignedPartitionsFieldInfo = typeof(MemoryBufferConsumer<int>)
            .GetField("_assignedPartitions", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var group1Customers = new IBufferConsumer<int>[5];
        for (var i = 0; i < 3; i++)
        {
            group1Customers[i] =
                queue.CreateConsumer(new BufferConsumerOptions { GroupName = "TestGroup1", AutoCommit = false });
        }

        for (var i = 0; i < 3; i++)
        {
            var partitions =
                (MemoryBufferPartition<int>[])assignedPartitionsFieldInfo.GetValue(group1Customers[i])!;
            Assert.Equal(3, partitions.Length);
        }

        group1Customers[3] =
            queue.CreateConsumer(new BufferConsumerOptions { GroupName = "TestGroup1", AutoCommit = false });

        for (var i = 0; i < 4; i++)
        {
            var partitions =
                (MemoryBufferPartition<int>[])assignedPartitionsFieldInfo.GetValue(group1Customers[i])!;
            Assert.Equal(i == 0 ? 3 : 2, partitions.Length);
        }

        group1Customers[4] =
            queue.CreateConsumer(new BufferConsumerOptions { GroupName = "TestGroup1", AutoCommit = false });

        for (var i = 0; i < 5; i++)
        {
            var partitions =
                (MemoryBufferPartition<int>[])assignedPartitionsFieldInfo.GetValue(group1Customers[i])!;

            Assert.Equal(i < 4 ? 2 : 1, partitions.Length);
        }
    }

    [Fact]
    public void Rebalance_If_Customer_Removed()
    {
        var queue = new MemoryBufferQueue<int>(9);

        var assignedPartitionsFieldInfo = typeof(MemoryBufferConsumer<int>)
            .GetField("_assignedPartitions", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var group1Customers = new IBufferConsumer<int>[9];

        for (var i = 0; i < 9; i++)
        {
            group1Customers[i] =
                queue.CreateConsumer(new BufferConsumerOptions { GroupName = "TestGroup1", AutoCommit = false });
        }

        for (var i = 0; i < 9; i++)
        {
            var partitions =
                (MemoryBufferPartition<int>[])assignedPartitionsFieldInfo.GetValue(group1Customers[i])!;
            Assert.Single(partitions);
        }

        queue.RemoveConsumer(group1Customers[0]);
        group1Customers = group1Customers[1..];
        for (var i = 0; i < 8; i++)
        {
            var partitions =
                (MemoryBufferPartition<int>[])assignedPartitionsFieldInfo.GetValue(group1Customers[i])!;
            Assert.Equal(i == 0 ? 2 : 1, partitions.Length);
        }
    }

    [Fact]
    public async Task Concurrent_Rebalance()
    {
        var customerCount = Environment.ProcessorCount;
        var partitionCount = Environment.ProcessorCount * 2;

        var queue = new MemoryBufferQueue<int>(partitionCount);

        var producer = queue.CreateProducer();

        var messageSize = MemoryBufferPartition<int>.SegmentLength * partitionCount;
        for (var i = 0; i < messageSize; i++)
        {
            await producer.ProduceAsync(i);
        }

        var assignedPartitionsFieldInfo = typeof(MemoryBufferConsumer<int>)
            .GetField("_assignedPartitions", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var customers = new IBufferConsumer<int>[customerCount];
        Parallel.For(0, customerCount, i =>
        {
            customers[i] =
                queue.CreateConsumer(new BufferConsumerOptions { GroupName = "TestGroup", AutoCommit = false });

            _ = Task.Run(async () =>
            {
                await foreach (var item in customers[i].ConsumeAsync())
                {
                }
            });
        });

        var allPartitions = new List<MemoryBufferPartition<int>>();

        for (var i = 0; i < customerCount; i++)
        {
            var partitions = (MemoryBufferPartition<int>[])assignedPartitionsFieldInfo.GetValue(customers[i])!;

            Assert.Equal(2, partitions.Length);
            allPartitions.AddRange(partitions);
        }

        var ids = allPartitions.Select(x => x.PartitionId).ToHashSet();
        Assert.Equal(partitionCount, ids.Count);
    }


    [Fact]
    public void Concurrent_Producer_Single_Partition()
    {
        var messageSize = MemoryBufferPartition<int>.SegmentLength * 4;

        var queue = new MemoryBufferQueue<int>(1);

        var countDownEvent = new CountdownEvent(messageSize);
        var customer = queue.CreateConsumer(new BufferConsumerOptions { GroupName = "TestGroup", AutoCommit = true });
        _ = Task.Run(async () =>
        {
            await foreach (var item in customer.ConsumeAsync())
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

        countDownEvent.Wait(TimeSpan.FromSeconds(10));
        if (countDownEvent.CurrentCount != 0)
        {
            throw new Exception("Not all messages consumed.");
        }
    }

    [Fact]
    public void Concurrent_Producer_Multiple_Partition()
    {
        var messageSize = MemoryBufferPartition<int>.SegmentLength * 4;

        var queue = new MemoryBufferQueue<int>(Environment.ProcessorCount);

        var customer = queue.CreateConsumer(new BufferConsumerOptions { GroupName = "TestGroup", AutoCommit = true });
        var countDownEvent = new CountdownEvent(messageSize);
        _ = Task.Run(async () =>
        {
            await foreach (var item in customer.ConsumeAsync())
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

        countDownEvent.Wait(TimeSpan.FromSeconds(10));
        if (countDownEvent.CurrentCount != 0)
        {
            throw new Exception("Not all messages consumed.");
        }
    }

    [Fact]
    public async Task Concurrent_Consumer_Single_Group()
    {
        var messageSize = MemoryBufferPartition<int>.SegmentLength * 4;
        var partitionNumber = Environment.ProcessorCount;
        var customerNumberPerGroup = Environment.ProcessorCount;

        var queue = new MemoryBufferQueue<int>(partitionNumber);
        var producer = queue.CreateProducer();
        for (var i = 0; i < messageSize; i++)
        {
            await producer.ProduceAsync(i);
        }

        var countDownEvent1 = new CountdownEvent(messageSize);

        for (var i = 0; i < customerNumberPerGroup; i++)
        {
            _ = Task.Run(async () =>
            {
                var consumer =
                    queue.CreateConsumer(new BufferConsumerOptions { GroupName = "TestGroup1", AutoCommit = true });
                await foreach (var item in consumer.ConsumeAsync())
                {
                    if (countDownEvent1.Signal())
                    {
                        break;
                    }
                }
            });
        }

        countDownEvent1.Wait(TimeSpan.FromSeconds(10));

        if (countDownEvent1.CurrentCount != 0)
        {
            throw new Exception("Not all messages consumed.");
        }
    }

    [Fact]
    public async Task Concurrent_Consumer_Multiple_Groups()
    {
        var messageSize = MemoryBufferPartition<int>.SegmentLength * 4;
        var partitionNumber = Environment.ProcessorCount;
        var customerNumberPerGroup = Environment.ProcessorCount;
        var groupNumber = 2;
        var countDownEvent = new CountdownEvent(messageSize * groupNumber);

        var queue = new MemoryBufferQueue<int>(partitionNumber);
        var producer = queue.CreateProducer();
        for (var i = 0; i < messageSize; i++)
        {
            await producer.ProduceAsync(i);
        }

        for (var groupIndex = 0; groupIndex < groupNumber; groupIndex++)
        {
            for (var i = 0; i < customerNumberPerGroup; i++)
            {
                var index = groupIndex;
                _ = Task.Run(async () =>
                {
                    var consumer =
                        queue.CreateConsumer(new BufferConsumerOptions
                        {
                            GroupName = "TestGroup" + (index + 1), AutoCommit = true
                        });
                    await foreach (var item in consumer.ConsumeAsync())
                    {
                        if (countDownEvent.Signal())
                        {
                            break;
                        }
                    }
                });
            }
        }

        try
        {
            countDownEvent.Wait(TimeSpan.FromSeconds(10));
        }
        catch
        {
            throw new Exception("Not all messages consumed.");
        }
    }

    [Fact]
    public void Concurrent_Producer_And_Concurrent_Consumer_Single_Group()
    {
        MemoryBufferPartition<int>.SegmentLength = 3;
        var messageSize = 100;
        var partitionNumber = Environment.ProcessorCount;
        var customerNumberPerGroup = Environment.ProcessorCount;

        var queue = new MemoryBufferQueue<int>(partitionNumber);

        var customerCountdownEvent = new CountdownEvent(messageSize);
        for (var i = 0; i < customerNumberPerGroup; i++)
        {
            _ = Task.Run(async () =>
            {
                var consumer =
                    queue.CreateConsumer(new BufferConsumerOptions { GroupName = "TestGroup1", AutoCommit = true });
                await foreach (var item in consumer.ConsumeAsync())
                {
                    if (customerCountdownEvent.Signal())
                    {
                        break;
                    }
                }
            });
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

        try
        {
            customerCountdownEvent.Wait(TimeSpan.FromSeconds(10));
        }
        catch
        {
            throw new Exception("Not all messages consumed.");
        }
    }

    // TODO: Fix this test
    // [Theory]
    // [InlineData(1)]
    // [InlineData(2)]
    // [InlineData(3)]
    public void Concurrent_Producer_And_Concurrent_Consumer_Multiple_Groups(int groupNumber)
    {
        var messageSize = MemoryBufferPartition<int>.SegmentLength * 4;
        var partitionNumber = Environment.ProcessorCount;
        var customerNumberPerGroup = Environment.ProcessorCount;

        var queue = new MemoryBufferQueue<int>(partitionNumber);

        var countdownEvent = new CountdownEvent(messageSize * groupNumber);

        for (var groupIndex = 0; groupIndex < groupNumber; groupIndex++)
        {
            for (var i = 0; i < customerNumberPerGroup; i++)
            {
                var index = groupIndex;
                _ = Task.Run(async () =>
                {
                    var consumer =
                        queue.CreateConsumer(new BufferConsumerOptions
                        {
                            GroupName = "TestGroup" + (index + 1), AutoCommit = true
                        });
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

        countdownEvent.Wait(TimeSpan.FromSeconds(10));
        if (countdownEvent.CurrentCount != 0)
        {
            throw new Exception("Not all messages consumed.");
        }
    }
}
