// Licensed to the .NET Core Community under one or more agreements.
// The.NET Core Community licenses this file to you under the MIT license.

using System.Reflection;
using Mocha.Core.Buffer;
using Mocha.Core.Buffer.Memory;

namespace Mocha.Core.Tests.Buffer.Memory;

public class MemoryBufferQueueTests
{
    [Fact]
    public async Task Produce_And_Consume()
    {
        var queue = new MemoryBufferQueue<int>(1);
        var producer = queue.CreateProducer();
        var consumer = queue.CreateConsumer(new BufferConsumerOptions { GroupName = "TestGroup", AutoCommit = false });

        var expectedValues = new int[10];
        for (var i = 0; i < 10; i++)
        {
            producer.Produce(i);
            expectedValues[i] = i;
        }

        var index = 0;
        await foreach (var item in consumer.ConsumeAsync())
        {
            Assert.Equal(expectedValues[index++], item);
            await consumer.CommitAsync();
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
            producer.Produce(i);
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
            producer.Produce(i);
            expectedValues[i] = i;
        }

        var index = 0;
        await foreach (var item in consumer.ConsumeAsync())
        {
            Assert.Equal(expectedValues[index++], item);
            await consumer.CommitAsync();
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

        producer.Produce(1);
        producer.Produce(2);

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

        producer.Produce(1);
        producer.Produce(2);

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

        await consumer.CommitAsync();

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

        producer.Produce(1);

        await task;
    }

    [Fact]
    public void Equal_distribution_load_balancing_strategy_for_consumers()
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
}
