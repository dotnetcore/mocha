// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Buffer.Memory;

internal sealed class MemoryBufferSegment<T>
{
    private readonly MemoryBufferPartition<T>.Offset _startOffset;
    private readonly MemoryBufferPartition<T>.Offset _endOffset;
    private readonly T[] _slots;
    private int _writePosition;

    public MemoryBufferSegment(int length, MemoryBufferPartition<T>.Offset startOffset)
    {
        _startOffset = startOffset;
        _endOffset = startOffset + (ulong)length;
        _slots = new T[length];
        _writePosition = -1;
    }

    public MemoryBufferSegment<T>? NextSegment { get; set; }

    public MemoryBufferPartition<T>.Offset StartOffset => _startOffset;

    public MemoryBufferPartition<T>.Offset EndOffset => _endOffset;

    public bool TryEnqueue(T item)
    {
        while (true)
        {
            var writePosition = Interlocked.Increment(ref _writePosition);
            if (writePosition >= _slots.Length)
            {
                return false;
            }

            _slots[writePosition] = item;
            return true;
        }
    }

    public bool TryGet(MemoryBufferPartition<T>.Offset offset, out T item)
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
}
