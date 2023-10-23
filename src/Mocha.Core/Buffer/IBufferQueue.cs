// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Buffer;

public interface IBufferQueue<T>
{
    IBufferProducer<T> CreateProducer();

    IBufferConsumer<T> CreateConsumer(BufferConsumerOptions options);
}
