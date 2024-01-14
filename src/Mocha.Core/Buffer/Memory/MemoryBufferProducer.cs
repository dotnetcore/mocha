// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Buffer.Memory;

internal sealed class MemoryBufferProducer<T>(string topicName, MemoryBufferPartition<T>[] partitions)
    : IBufferProducer<T>
{
    private uint _partitionIndex;

    public string TopicName { get; } = topicName;

    public ValueTask ProduceAsync(T item)
    {
        var partition = SelectPartition();
        partition.Enqueue(item);
        return ValueTask.CompletedTask;
    }

    private MemoryBufferPartition<T> SelectPartition()
    {
        var index = (Interlocked.Increment(ref _partitionIndex) - 1) % partitions.Length;
        return partitions[index];
    }
}
