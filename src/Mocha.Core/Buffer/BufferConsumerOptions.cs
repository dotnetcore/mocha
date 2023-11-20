// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Buffer;

public class BufferConsumerOptions
{
    public string TopicName { get; init; } = default!;

    public string GroupName { get; init; } = default!;

    public bool AutoCommit { get; init; }
}
