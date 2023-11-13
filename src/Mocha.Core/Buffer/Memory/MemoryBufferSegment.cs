// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Mocha.Core.Buffer.Memory;

[DebuggerDisplay("StartOffset = {StartOffset}, EndOffset = {EndOffset}")]
[DebuggerTypeProxy(typeof(MemoryBufferSegment<>.DebugView))]
internal sealed class MemoryBufferSegment<T>
{
    private readonly MemoryBufferPartitionOffset _startOffset;
    private readonly MemoryBufferPartitionOffset _endOffset;
    private readonly T[] _slots;
    private int _writePosition;

    public MemoryBufferSegment(int length, MemoryBufferPartitionOffset startOffset)
    {
        _startOffset = startOffset;
        _endOffset = startOffset + (ulong)(length - 1);
        _slots = new T[length];
        _writePosition = -1;
    }

    private MemoryBufferSegment(T[] slots, MemoryBufferPartitionOffset startOffset)
    {
        _startOffset = startOffset;
        _endOffset = startOffset + (ulong)(slots.Length - 1);
        _slots = slots;
        _writePosition = -1;
    }

    public MemoryBufferSegment<T>? NextSegment { get; set; }

    public MemoryBufferPartitionOffset StartOffset => _startOffset;

    public MemoryBufferPartitionOffset EndOffset => _endOffset;

    public int Capacity => _slots.Length;

    public int Count => _writePosition + 1;

    public bool TryEnqueue(T item)
    {
        var writePosition = Interlocked.Increment(ref _writePosition);
        if (writePosition >= _slots.Length)
        {
            return false;
        }

        _slots[writePosition] = item;
        return true;
    }

    public bool TryGet(MemoryBufferPartitionOffset offset, out T item)
    {
        if (offset < _startOffset || offset > _endOffset)
        {
            item = default!;
            return false;
        }

        var readPosition = (offset - _startOffset).ToUInt64();

        if (_writePosition < 0 || readPosition > (ulong)_writePosition)
        {
            item = default!;
            return false;
        }

        item = _slots[readPosition];
        return true;
    }

    public MemoryBufferSegment<T> RecycleSlots(MemoryBufferPartitionOffset startOffset)
    {
        Array.Clear(_slots, 0, _slots.Length);
        return new MemoryBufferSegment<T>(_slots, startOffset);
    }

    private class DebugView
    {
        private readonly MemoryBufferSegment<T> _segment;

        public DebugView(MemoryBufferSegment<T> segment)
        {
            _segment = segment;
        }

        public MemoryBufferPartitionOffset StartOffset => _segment._startOffset;

        public MemoryBufferPartitionOffset EndOffset => _segment._endOffset;

        public int Capacity => _segment.Capacity;

        public int Count => _segment.Count;

        public T[] Items => _segment._slots.Take(_segment._writePosition + 1).ToArray();
    }
}
