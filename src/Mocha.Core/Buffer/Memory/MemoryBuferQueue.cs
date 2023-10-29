// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Buffer.Memory;

internal sealed class MemoryBufferQueue<T> : IBufferQueue<T>
{
    private readonly MemoryBufferPartition<T>[] _partitions;
    private readonly int _partitionNumber;
    // Consider that the frequency of creating and deleting consumers will not be very high,
    // so the lock is relatively coarse-grained.
    private readonly object _consumersLock;
    private readonly Dictionary<string /* GroupName */, List<MemoryBufferConsumer<T>>> _consumers;

    public MemoryBufferQueue(int partitionNumber)
    {
        _partitionNumber = partitionNumber;
        _partitions = new MemoryBufferPartition<T>[partitionNumber];
        _consumers = new Dictionary<string, List<MemoryBufferConsumer<T>>>();
        _consumersLock = new object();

        for (var i = 0; i < partitionNumber; i++)
        {
            _partitions[i] = new MemoryBufferPartition<T>();
        }
    }

    public MemoryBufferPartition<T>[] Partitions => _partitions;

    public IBufferProducer<T> CreateProducer() => new MemoryBufferProducer<T>(this);

    public IBufferConsumer<T> CreateConsumer(BufferConsumerOptions options)
    {
        var groupName = options.GroupName;
        if (groupName == null)
        {
            throw new ArgumentNullException(nameof(groupName));
        }

        lock (_consumersLock)
        {
            _consumers.TryGetValue(groupName, out var currentGroupConsumers);
            if (currentGroupConsumers == null)
            {
                currentGroupConsumers = new List<MemoryBufferConsumer<T>>();
                _consumers.Add(groupName, currentGroupConsumers);
            }

            if (currentGroupConsumers.Count >= _partitionNumber)
            {
                throw new InvalidOperationException(
                    $"Maximum number of consumers reached for group {groupName}, no more than {_partitionNumber} consumers are allowed.");
            }

            var consumer = new MemoryBufferConsumer<T>(options, this);
            currentGroupConsumers.Add(consumer);

            Rebalance(currentGroupConsumers);

            return consumer;
        }
    }

    public void RemoveConsumer(IBufferConsumer<T> consumer)
    {
        lock (_consumersLock)
        {
            var groupName = consumer.GroupName;
            if (!_consumers.TryGetValue(groupName, out var currentGroupConsumers))
            {
                throw new InvalidOperationException($"Group {groupName} not found.");
            }

            if (!currentGroupConsumers.Remove((MemoryBufferConsumer<T>)consumer))
            {
                throw new InvalidOperationException($"Consumer not found in group {groupName}.");
            }

            Rebalance(currentGroupConsumers);
        }
    }

    private void Rebalance(List<MemoryBufferConsumer<T>> consumers)
    {
        var consumersCount = consumers.Count;
        if (consumersCount == 0)
        {
            return;
        }

        var partitionsPerConsumer = _partitionNumber / consumersCount;

        var partitionsRemainder = _partitionNumber % consumersCount;

        var startIndex = 0;
        foreach (var consumer in consumers)
        {
            var extraPartitions = partitionsRemainder > 0 ? 1 : 0;
            var endIndex = startIndex + partitionsPerConsumer + extraPartitions;
            var partitions = _partitions[startIndex..endIndex];

            consumer.AssignPartitions(partitions);

            startIndex = endIndex;

            if (partitionsRemainder > 0)
            {
                partitionsRemainder--;
            }
        }
    }
}
