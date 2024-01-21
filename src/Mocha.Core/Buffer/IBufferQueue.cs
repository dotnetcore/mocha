// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Buffer;

public interface IBufferQueue
{
    /// <summary>
    /// Create a producer for the specified topic.
    /// </summary>
    /// <param name="topicName">The topic name.</param>
    /// <typeparam name="T">The type of the item.</typeparam>
    /// <returns>The producer.</returns>
    IBufferProducer<T> CreateProducer<T>(string topicName);

    /// <summary>
    /// Create a consumer for the specified topic.
    /// This method can only be called once for each consumer group within the same topic.
    /// Use the <see cref="CreateConsumers{T}"/> method to create multiple consumers.
    /// </summary>
    /// <param name="options">The consumer options.</param>
    /// <typeparam name="T">The type of the item.</typeparam>
    /// <returns>The consumer.</returns>
    /// <exception cref="ArgumentNullException">The topic name is.</exception>
    /// <exception cref="ArgumentException">The group name is empty.</exception>
    /// <exception cref="InvalidOperationException">The consumer group has been created.</exception>
    IBufferConsumer<T> CreateConsumer<T>(BufferConsumerOptions options);

    /// <summary>
    /// Create multiple consumers for the specified topic.
    /// This method can only be called once for each consumer group within the same topic.
    /// </summary>
    /// <param name="options">The consumer options.</param>
    /// <param name="consumerNumber">The number of consumers.</param>
    /// <typeparam name="T">The type of the item.</typeparam>
    /// <returns>The consumers.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The number of consumers must be greater than 0 and cannot be greater than the number of partitions.</exception>
    /// <exception cref="ArgumentNullException">The topic name is.</exception>
    /// <exception cref="ArgumentException">The group name is empty.</exception>
    /// <exception cref="InvalidOperationException">The consumer group has been created.</exception>
    IEnumerable<IBufferConsumer<T>> CreateConsumers<T>(BufferConsumerOptions options, int consumerNumber);
}
