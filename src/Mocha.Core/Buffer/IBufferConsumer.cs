// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Buffer;

public interface IBufferConsumer<out T>
{
    string TopicName { get; }

    string GroupName { get; }

    IAsyncEnumerable<IEnumerable<T>> ConsumeAsync(CancellationToken cancellationToken = default);

    ValueTask CommitAsync();
}
