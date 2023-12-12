// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Storage;

public static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddStorage(this IServiceCollection services, Action<StorageOptionsBuilder> configure)
    {
        configure(new StorageOptionsBuilder(services));
        return services;
    }
}
