// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Buffer;

internal interface IBufferQueue<T>
{
    string TopicName { get; }

    IBufferProducer<T> CreateProducer();

    IBufferConsumer<T> CreateConsumer(BufferConsumerOptions options);

    IEnumerable<IBufferConsumer<T>> CreateConsumers(BufferConsumerOptions options, int consumerNumber);
}
