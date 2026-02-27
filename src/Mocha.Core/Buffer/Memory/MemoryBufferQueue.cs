// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Buffer.Memory;

internal sealed class MemoryBufferQueue<T> : IBufferQueue<T>
{
    private readonly MemoryBufferPartition<T>[] _partitions;
    private readonly int _partitionNumber;

    private readonly IBufferProducer<T> _producer;

    // Consider that the frequency of creating consumers will not be very high,
    // so the lock is relatively coarse-grained.
    private readonly object _consumersLock;
    private readonly Dictionary<string /* GroupName */, List<MemoryBufferConsumer<T>>> _consumers;

    public MemoryBufferQueue(string topicName, int partitionNumber)
    {
        TopicName = topicName;
        _partitionNumber = partitionNumber;
        _partitions = new MemoryBufferPartition<T>[partitionNumber];
        for (var i = 0; i < partitionNumber; i++)
        {
            _partitions[i] = new MemoryBufferPartition<T>(i);
        }

        _producer = new MemoryBufferProducer<T>(topicName, _partitions);

        _consumers = new Dictionary<string, List<MemoryBufferConsumer<T>>>();
        _consumersLock = new object();
    }

    public string TopicName { get; }

    public IBufferProducer<T> CreateProducer() => _producer;

    public IBufferConsumer<T> CreateConsumer(BufferConsumerOptions options)
    {
        var consumers = CreateConsumers(options, 1);
        return consumers.Single();
    }

    public IEnumerable<IBufferConsumer<T>> CreateConsumers(BufferConsumerOptions options, int consumerNumber)
    {
        if (consumerNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(consumerNumber),
                "The number of consumers must be greater than 0.");
        }

        if (consumerNumber > _partitionNumber)
        {
            throw new ArgumentOutOfRangeException(nameof(consumerNumber),
                "The number of consumers cannot be greater than the number of partitions.");
        }

        var groupName = options.GroupName;
        ArgumentException.ThrowIfNullOrEmpty(groupName, nameof(options.GroupName));

        lock (_consumersLock)
        {
            if (_consumers.ContainsKey(groupName))
            {
                throw new InvalidOperationException($"The consumer group '{groupName}' already exists.");
            }

            var consumers = new List<MemoryBufferConsumer<T>>();
            for (var i = 0; i < consumerNumber; i++)
            {
                var consumer = new MemoryBufferConsumer<T>(options);
                consumers.Add(consumer);
            }

            AssignPartitions(consumers);

            _consumers.Add(groupName, consumers);
            return consumers;
        }
    }

    private void AssignPartitions(List<MemoryBufferConsumer<T>> consumers)
    {
        var consumerNumber = consumers.Count;
        var partitionsPerConsumer = _partitionNumber / consumerNumber;
        var partitionsRemainder = _partitionNumber % consumerNumber;
        var partitionStartIndex = 0;
        foreach (var consumer in consumers)
        {
            var extraPartitions = partitionsRemainder > 0 ? 1 : 0;
            var partitionEndIndex = partitionStartIndex
                                    + partitionsPerConsumer
                                    + extraPartitions;
            var partitions = _partitions[partitionStartIndex..partitionEndIndex];
            consumer.AssignPartitions(partitions);

            partitionStartIndex = partitionEndIndex;

            if (partitionsRemainder > 0)
            {
                partitionsRemainder--;
            }
        }
    }
}
