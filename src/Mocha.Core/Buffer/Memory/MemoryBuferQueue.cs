// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Buffer.Memory;

internal sealed class MemoryBufferQueue<T> : IBufferQueue<T>
{
    private readonly MemoryBufferPartition<T>[] _partitions;
    private readonly int _partitionNumber;

    private readonly IBufferProducer<T> _producer;

    // Consider that the frequency of creating and deleting consumers will not be very high,
    // so the lock is relatively coarse-grained.
    private readonly object _consumersLock;
    private readonly Dictionary<string /* GroupName */, List<MemoryBufferConsumer<T>>> _consumers;

    public MemoryBufferQueue(int partitionNumber)
    {
        _partitionNumber = partitionNumber;
        _partitions = new MemoryBufferPartition<T>[partitionNumber];
        for (var i = 0; i < partitionNumber; i++)
        {
            _partitions[i] = new MemoryBufferPartition<T>();
        }

        _producer = new MemoryBufferProducer<T>(_partitions);

        _consumers = new Dictionary<string, List<MemoryBufferConsumer<T>>>();
        _consumersLock = new object();
    }

    public IBufferProducer<T> CreateProducer() => _producer;

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

            var newConsumer = new MemoryBufferConsumer<T>(options, this);
            currentGroupConsumers.Add(newConsumer);

            Rebalance(currentGroupConsumers);

            return newConsumer;
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

        foreach (var partition in _partitions)
        {
            partition.ClearRegisteredConsumers();
        }

        if (consumersCount == 1)
        {
            consumers[0].AssignPartitions(_partitions);
            return;
        }

        foreach (var consumer in consumers)
        {
            consumer.Pause();
        }

        var partitionsPerConsumer = _partitionNumber / consumersCount;

        var partitionsRemainder = _partitionNumber % consumersCount;

        var partitionsBeingConsumed = consumers
            .Where(c => c.IsConsuming)
            .Select(c => c.PartitionBeingConsumed)
            .ToHashSet();

        var reassignAllowedPartitions =
            _partitions.Where(p => !partitionsBeingConsumed.Contains(p)).ToArray();

        var startIndex = 0;
        foreach (var consumer in consumers)
        {
            var isConsuming = consumer.IsConsuming;
            var partitionBeingConsumed = consumer.PartitionBeingConsumed!;
            var extraPartitions = partitionsRemainder > 0 ? 1 : 0;
            var endIndex = startIndex
                           + partitionsPerConsumer
                           + (isConsuming ? -1 : 0)
                           + extraPartitions;

            var isPartitionEnough = endIndex == startIndex;
            if (isPartitionEnough)
            {
                consumer.AssignPartitions(partitionBeingConsumed);
                continue;
            }

            var partitions = reassignAllowedPartitions[startIndex..endIndex];

            if (isConsuming)
            {
                partitions = partitions.Append(partitionBeingConsumed).ToArray();
            }

            consumer.AssignPartitions(partitions);

            startIndex = endIndex;

            if (partitionsRemainder > 0)
            {
                partitionsRemainder--;
            }
        }

        foreach (var consumer in consumers)
        {
            consumer.Resume();
        }
    }
}
