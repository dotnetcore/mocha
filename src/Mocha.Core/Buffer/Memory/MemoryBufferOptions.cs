// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Core.Buffer.Memory;

public class MemoryBufferOptions
{
    private readonly IServiceCollection _services;

    public MemoryBufferOptions(IServiceCollection services)
    {
        _services = services;
    }

    public MemoryBufferOptions AddTopic<T>(int partitionNumber)
    {
        _services.AddSingleton<IBufferQueue<T>>(new MemoryBufferQueue<T>(partitionNumber));
        return this;
    }
}
