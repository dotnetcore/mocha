// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Buffer;

namespace Microsoft.Extensions.DependencyInjection;

public static class BufferServiceCollectionExtensions
{
    public static IServiceCollection AddBuffer(
        this IServiceCollection services,
        Action<BufferOptionsBuilder> configure)
    {
        configure(new BufferOptionsBuilder(services));
        return services;
    }
}
