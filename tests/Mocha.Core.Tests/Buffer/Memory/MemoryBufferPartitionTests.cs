// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Reflection;
using Mocha.Core.Buffer.Memory;

namespace Mocha.Core.Tests.Buffer.Memory;

public class MemoryBufferPartitionTests
{
    [Fact]
    public void Enqueue_And_TryPull()
    {
        var partition = new MemoryBufferPartition<int>(0, 2);

        for (var i = 0; i < 12; i++)
        {
            partition.Enqueue(i);
        }

        Assert.True(partition.TryPull("TestGroup", 4, out var items));
        Assert.Equal(new[] { 0, 1, 2, 3 }, items);
        partition.Commit("TestGroup");

        Assert.True(partition.TryPull("TestGroup", 3, out items));
        Assert.Equal(new[] { 4, 5, 6 }, items);
        partition.Commit("TestGroup");

        Assert.True(partition.TryPull("TestGroup", 2, out items));
        Assert.Equal(new[] { 7, 8 }, items);
        partition.Commit("TestGroup");

        Assert.True(partition.TryPull("TestGroup", 4, out items));
        Assert.Equal(new[] { 9, 10, 11 }, items);
        partition.Commit("TestGroup");

        Assert.False(partition.TryPull("TestGroup", 2, out _));

        partition.Enqueue(12);

        Assert.True(partition.TryPull("TestGroup", 3, out items));
        Assert.Equal(new[] { 12 }, items);
    }

    [Fact]
    public void Repeatable_Pull_If_Not_Commit()
    {
        var partition = new MemoryBufferPartition<int>(0, 2);

        for (var i = 0; i < 11; i++)
        {
            partition.Enqueue(i);
        }

        Assert.True(partition.TryPull("TestGroup", 4, out var items));
        Assert.Equal(new[] { 0, 1, 2, 3 }, items);

        Assert.True(partition.TryPull("TestGroup", 3, out items));
        Assert.Equal(new[] { 0, 1, 2 }, items);

        partition.Commit("TestGroup");

        Assert.True(partition.TryPull("TestGroup", 3, out items));
        Assert.Equal(new[] { 3, 4, 5 }, items);

        Assert.True(partition.TryPull("TestGroup", 5, out items));
        Assert.Equal(new[] { 3, 4, 5, 6, 7 }, items);

        partition.Commit("TestGroup");

        Assert.True(partition.TryPull("TestGroup", 6, out items));
        Assert.Equal(new[] { 8, 9, 10 }, items);

        Assert.True(partition.TryPull("TestGroup", 3, out items));
        Assert.Equal(new[] { 8, 9, 10 }, items);

        partition.Commit("TestGroup");

        Assert.False(partition.TryPull("TestGroup", 2, out _));
    }

    [Fact]
    public void Segment_Will_Be_Recycled_If_All_Consumers_Consumed_Single_Group()
    {
        var partition = new MemoryBufferPartition<int>(0, 3);

        for (var i = 0; i < 9; i++)
        {
            partition.Enqueue(i);
        }

        var segments1 = GetSegments(partition);

        for (var i = 0; i < 2; i++)
        {
            Assert.True(partition.TryPull("TestGroup", 3, out var items));
            Assert.Equal(new[] { i * 3, i * 3 + 1, i * 3 + 2 }, items);
            partition.Commit("TestGroup");
        }

        partition.Enqueue(9);

        for (var i = 0; i < 4; i++)
        {
            Assert.True(partition.TryPull("TestGroup", 1, out var items));
            Assert.Equal(i + 6, items.Single());
            partition.Commit("TestGroup");
        }

        var segments2 = GetSegments(partition);

        Assert.True(GetSlots(segments1[1]) == GetSlots(segments2[1]));
        Assert.True(GetSlots(segments1[2]) == GetSlots(segments2[0]));
    }

    [Fact]
    public void Segment_Will_Be_Recycled_If_All_Consumers_Consumed_MultipleGroup()
    {
        var partition = new MemoryBufferPartition<int>(0, 3);

        for (var i = 0; i < 9; i++)
        {
            partition.Enqueue(i);
        }

        var segments1 = GetSegments(partition);

        for (var i = 0; i < 3; i++)
        {
            Assert.True(partition.TryPull("TestGroup1", 1, out _));
            partition.Commit("TestGroup1");
        }

        for (var i = 0; i < 2; i++)
        {
            Assert.True(partition.TryPull("TestGroup2", 3, out _));
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
        var partition = new MemoryBufferPartition<int>(0, 3);

        for (var i = 0; i < 6; i++)
        {
            partition.Enqueue(i);
        }

        var segments1 = GetSegments(partition);

        for (var i = 0; i < 3; i++)
        {
            Assert.True(partition.TryPull("TestGroup1", 1, out _));
            partition.Commit("TestGroup1");
        }

        for (var i = 0; i < 2; i++)
        {
            Assert.True(partition.TryPull("TestGroup2", 1, out _));
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
