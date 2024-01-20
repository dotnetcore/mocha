// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Core.Buffer;

internal class BufferQueue(IServiceProvider serviceProvider) : IBufferQueue
{
    public IBufferProducer<T> CreateProducer<T>(string topicName)
    {
        ArgumentException.ThrowIfNullOrEmpty(topicName, nameof(topicName));
        var queue = serviceProvider.GetRequiredKeyedService<IBufferQueue<T>>(topicName);
        return queue.CreateProducer();
    }

    public IBufferConsumer<T> CreateConsumer<T>(BufferConsumerOptions options)
    {
        ArgumentException.ThrowIfNullOrEmpty(options.TopicName, nameof(options.TopicName));
        var queue = serviceProvider.GetRequiredKeyedService<IBufferQueue<T>>(options.TopicName);
        return queue.CreateConsumer(options);
    }

    public IEnumerable<IBufferConsumer<T>> CreateConsumers<T>(BufferConsumerOptions options, int consumerNumber)
    {
        ArgumentException.ThrowIfNullOrEmpty(options.TopicName, nameof(options.TopicName));
        var queue = serviceProvider.GetRequiredKeyedService<IBufferQueue<T>>(options.TopicName);
        return queue.CreateConsumers(options, consumerNumber);
    }
}
