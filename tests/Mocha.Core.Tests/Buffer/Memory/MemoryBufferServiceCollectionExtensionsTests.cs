// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Mocha.Core.Buffer;

namespace Mocha.Core.Tests.Buffer.Memory;

public class MemoryBufferServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMemoryBuffer()
    {
        var services = new ServiceCollection();
        services.AddBuffer(options => options.UseMemory(builder =>
            builder
                .AddTopic<int>("topic1", 1)
                .AddTopic<int>("topic2", 2)
        ));
        var provider = services.BuildServiceProvider();
        var bufferQueue = provider.GetRequiredService<IBufferQueue>();

        var topic1Producer = bufferQueue.CreateProducer<int>("topic1");
        var topic1Consumer =
            bufferQueue.CreateConsumer<int>(new BufferConsumerOptions { TopicName = "topic1", GroupName = "test" });

        var topic2Producer = bufferQueue.CreateProducer<int>("topic2");
        var topic2Consumers =
            bufferQueue.CreateConsumers<int>(new BufferConsumerOptions { TopicName = "topic2", GroupName = "test" }, 2)
                .ToList();

        Assert.Equal("topic1", topic1Producer.TopicName);
        Assert.Equal("topic1", topic1Consumer.TopicName);
        Assert.Equal("topic2", topic2Producer.TopicName);
        Assert.Equal(2, topic2Consumers.Count());
        foreach (var consumer in topic2Consumers)
        {
            Assert.Equal("topic2", consumer.TopicName);
        }
    }

    [Fact]
    public async Task No_Consumption_If_TopicName_Not_Match()
    {
        var services = new ServiceCollection();
        services.AddBuffer(options => options.UseMemory(builder =>
            builder
                .AddTopic<int>("topic1", 1)
                .AddTopic<int>("topic2", 2)
        ));

        var provider = services.BuildServiceProvider();
        var bufferQueue = provider.GetRequiredService<IBufferQueue>();

        var topic1Producer = bufferQueue.CreateProducer<int>("topic1");
        var topic1Consumer =
            bufferQueue.CreateConsumer<int>(new BufferConsumerOptions { TopicName = "topic1", GroupName = "test" });
        var topic2Consumer =
            bufferQueue.CreateConsumer<int>(new BufferConsumerOptions { TopicName = "topic2", GroupName = "test" });

        await topic1Producer.ProduceAsync(1);

        await foreach (var item in topic1Consumer.ConsumeAsync())
        {
            Assert.Equal(1, item.Single());
            break;
        }

        _ = Task.Run(async () =>
        {
            await foreach (var item in topic2Consumer.ConsumeAsync())
            {
                Assert.True(false, "Should not consume any item.");
            }
        });

        await Task.Delay(100);
    }

    [Fact]
    public void Throw_If_TopicName_Not_Registered()
    {
        var services = new ServiceCollection();
        services.AddBuffer(options => options.UseMemory(builder =>
            builder
                .AddTopic<int>("topic1", 1)
                .AddTopic<int>("topic2", 2)
        ));

        var provider = services.BuildServiceProvider();
        var bufferQueue = provider.GetRequiredService<IBufferQueue>();

        Assert.Throws<ArgumentException>(() => bufferQueue.CreateProducer<int>("topic3"));
        Assert.Throws<ArgumentException>(() =>
            bufferQueue.CreateConsumer<int>(new BufferConsumerOptions { TopicName = "topic3", GroupName = "test" }));
        Assert.Throws<ArgumentException>(() =>
            bufferQueue.CreateConsumers<int>(
                new BufferConsumerOptions { TopicName = "topic3", GroupName = "test" },
                2));
    }
}
