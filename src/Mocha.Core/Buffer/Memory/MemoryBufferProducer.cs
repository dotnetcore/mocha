// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Buffer.Memory;

internal sealed class MemoryBufferProducer<T> : IBufferProducer<T>
{
    private readonly MemoryBufferQueue<T> _queue;

    private uint _partitionIndex;

    public MemoryBufferProducer(MemoryBufferQueue<T> queue)
    {
        _queue = queue;
    }

    public void Produce(T item)
    {
        var partition = SelectPartition();
        partition.Enqueue(item);
    }

    private MemoryBufferPartition<T> SelectPartition()
    {
        var partitions = _queue.Partitions;
        var index = (Interlocked.Increment(ref _partitionIndex) - 1) % partitions.Length;
        return partitions[index];
    }
}
