// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Reflection;
using Mocha.Core.Buffer.Memory;

namespace Mocha.Core.Tests.Buffer.Memory;

public class MemoryBufferPartitionTests
{
    [Fact]
    public void Segment_Will_Be_Recycled_If_All_Consumers_Consumed_Single_Group()
    {
        MemoryBufferPartition<int>.SegmentLength = 3;

        var partition = new MemoryBufferPartition<int>();

        for (var i = 0; i < 9; i++)
        {
            partition.Enqueue(i);
        }

        var segments1 = GetSegments(partition);

        for (var i = 0; i < 6; i++)
        {
            partition.TryPull("TestGroup", out var item);
            Assert.Equal(i, item);
            partition.Commit("TestGroup");
        }

        partition.Enqueue(9);

        for (var i = 0; i < 4; i++)
        {
            partition.TryPull("TestGroup", out var item);
            Assert.Equal(i + 6, item);
            partition.Commit("TestGroup");
        }

        var segments2 = GetSegments(partition);

        Assert.True(GetSlots(segments1[1]) == GetSlots(segments2[1]));
        Assert.True(GetSlots(segments1[2]) == GetSlots(segments2[0]));
    }

    [Fact]
    public void Segment_Will_Be_Recycled_If_All_Consumers_Consumed_MultipleGroup()
    {
        MemoryBufferPartition<int>.SegmentLength = 3;

        var partition = new MemoryBufferPartition<int>();

        for (var i = 0; i < 9; i++)
        {
            partition.Enqueue(i);
        }

        var segments1 = GetSegments(partition);

        for (var i = 0; i < 3; i++)
        {
            Assert.True(partition.TryPull("TestGroup1", out var item));
            partition.Commit("TestGroup1");
        }

        for (var i = 0; i < 6; i++)
        {
            Assert.True(partition.TryPull("TestGroup2", out var item));
            partition.Commit("TestGroup2");
        }

        partition.Enqueue(9);

        var segments2 = GetSegments(partition);

        Assert.True(GetSlots(segments1[1]) == GetSlots(segments2[0]));
        Assert.True(GetSlots(segments1[0]) == GetSlots(segments2[2]));
    }

    [Fact]
    public void Segment_Will_Not_Be_Recycled_If_Not_All_Consumers_Consumed_MultipleGroup()
    {
        MemoryBufferPartition<int>.SegmentLength = 3;

        var partition = new MemoryBufferPartition<int>();

        for (var i = 0; i < 6; i++)
        {
            partition.Enqueue(i);
        }

        var segments1 = GetSegments(partition);

        for (var i = 0; i < 3; i++)
        {
            Assert.True(partition.TryPull("TestGroup1", out var item));
            partition.Commit("TestGroup1");
        }

        for (var i = 0; i < 2; i++)
        {
            Assert.True(partition.TryPull("TestGroup2", out var item));
            partition.Commit("TestGroup2");
        }

        partition.Enqueue(7);

        var segments2 = GetSegments(partition);

        Assert.Equal(GetSlots(segments1[0]), GetSlots(segments2[0]));
        Assert.Equal(GetSlots(segments1[1]), GetSlots(segments2[1]));
    }

    private List<MemoryBufferSegment<int>> GetSegments(MemoryBufferPartition<int> partition)
    {
        var head = typeof(MemoryBufferPartition<>)
            .MakeGenericType(typeof(int))
            .GetField("_head", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(partition) as MemoryBufferSegment<int>;

        var segments = new List<MemoryBufferSegment<int>>();
        var segment = head;
        while (segment != null)
        {
            segments.Add(segment);
            segment = segment.NextSegment;
        }

        return segments;
    }

    private T[]? GetSlots<T>(MemoryBufferSegment<T> segment)
    {
        return typeof(MemoryBufferSegment<>)
            .MakeGenericType(typeof(T))
            .GetField("_slots", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(segment) as T[];
    }
}
