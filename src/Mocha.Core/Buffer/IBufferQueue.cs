// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Buffer;

public interface IBufferQueue
{
    IBufferProducer<T> CreateProducer<T>(string topicName);

    IBufferConsumer<T> CreateConsumer<T>(BufferConsumerOptions options);

    IEnumerable<IBufferConsumer<T>> CreateConsumers<T>(BufferConsumerOptions options, int consumerNumber);
}
