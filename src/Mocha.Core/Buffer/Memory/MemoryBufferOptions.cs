// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Core.Buffer.Memory;

public class MemoryBufferOptions(IServiceCollection services)
{
    public MemoryBufferOptions AddTopic<T>(string topicName, int partitionNumber)
    {
        services.AddKeyedSingleton<IBufferQueue<T>>(topicName, new MemoryBufferQueue<T>(topicName, partitionNumber));
        return this;
    }
}
