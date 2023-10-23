// Licensed to the .NET Core Community under one or more agreements.
// The.NET Core Community licenses this file to you under the MIT license.

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
}
