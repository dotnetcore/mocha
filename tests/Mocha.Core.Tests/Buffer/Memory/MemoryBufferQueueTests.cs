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
    // 使用均分的方式，将每个消费者分配到的分区数量尽可能均匀
    public async Task Equal_distribution_load_balancing_strategy_for_consumers()
    {
        var queue = new MemoryBufferQueue<int>(18);

        var fieldInfo = typeof(MemoryBufferConsumer<int>)
            .GetField("_assignedPartitions", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var customers = new IBufferConsumer<int>[4];

        for (var i = 0; i < 4; i++)
        {
            customers[i] = queue.CreateConsumer(new BufferConsumerOptions { GroupName = $"TestGroup{i}", AutoCommit = false });
        }

        for (var i = 0; i < 3; i++)
        {
            var partitions = (MemoryBufferPartition<int>[])fieldInfo.GetValue(customers[i])!;
            Assert.Equal(4, partitions.Length);
        }

        var partitionsForLastCustomer = (MemoryBufferPartition<int>[])fieldInfo.GetValue(customers[3])!;
        Assert.Equal(6, partitionsForLastCustomer.Length);
    }
}
