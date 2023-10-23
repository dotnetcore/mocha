// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Buffer.Memory;

internal sealed class MemoryBufferQueue<T> : IBufferQueue<T>
{
    private readonly MemoryBufferPartition<T>[] _partitions;
    private readonly int _partitionNumber;
    private readonly List<MemoryBufferConsumer<T>> _consumers;
    private readonly object _rebalanceLock;

    public MemoryBufferQueue(int partitionNumber)
    {
        _partitionNumber = partitionNumber;
        _partitions = new MemoryBufferPartition<T>[partitionNumber];
        _consumers = new List<MemoryBufferConsumer<T>>();
        _rebalanceLock = new object();

        for (var i = 0; i < partitionNumber; i++)
        {
            _partitions[i] = new MemoryBufferPartition<T>();
        }
    }

    public MemoryBufferPartition<T>[] Partitions => _partitions;

    public IBufferProducer<T> CreateProducer() => new MemoryBufferProducer<T>(this);

    public IBufferConsumer<T> CreateConsumer(BufferConsumerOptions options)
    {
        lock (_rebalanceLock)
        {
            if (_consumers.Count >= _partitionNumber)
            {
                throw new InvalidOperationException("Maximum number of consumers reached, cannot create more.");
            }

            var consumer = new MemoryBufferConsumer<T>(options, this);
            _consumers.Add(consumer);

            Rebalance();

            return consumer;
        }
    }

    public void RemoveConsumer(IBufferConsumer<T> consumer)
    {
        lock (_rebalanceLock)
        {
            if (!_consumers.Remove((MemoryBufferConsumer<T>)consumer))
            {
                throw new InvalidOperationException("Consumer not found.");
            }

            Rebalance();
        }
    }

    private void Rebalance()
    {
        lock (_rebalanceLock)
        {
            var partitionsPerConsumer = _partitionNumber / _consumers.Count;
            var partitionsPerConsumerRemainder = _partitionNumber % _consumers.Count;

            var partitionIndex = 0;
            foreach (var consumer in _consumers)
            {
                var partitions = new MemoryBufferPartition<T>[partitionsPerConsumer];
                for (var i = 0; i < partitionsPerConsumer; i++)
                {
                    partitions[i] = _partitions[partitionIndex++];
                }

                consumer.AssignPartitions(partitions);
            }

            if (partitionsPerConsumerRemainder > 0)
            {
                var partitions = new MemoryBufferPartition<T>[partitionsPerConsumerRemainder];
                for (var i = 0; i < partitionsPerConsumerRemainder; i++)
                {
                    partitions[i] = _partitions[partitionIndex++];
                }

                _consumers[^1].AssignPartitions(partitions);
            }
        }
    }
}
