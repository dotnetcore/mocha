// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Buffer.Memory;

namespace Mocha.Core.Buffer;

public static class BufferOptionsBuilderExtensions
{
    public static BufferOptionsBuilder UseMemory(
        this BufferOptionsBuilder builder,
        Action<MemoryBufferOptions> configure)
    {
        var options = new MemoryBufferOptions(builder.Services);
        configure(options);

        return builder;
    }
}
