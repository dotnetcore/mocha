// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Core.Storage;
using Mocha.Storage.EntityFrameworkStorage;

namespace Mocha.Storage;

public static class EntityFrameworkCoreOptionsBuilderExtensions
{
    public static IServiceCollection AddStorage(
        this IServiceCollection services,
        Action<StorageOptionsBuilder> configure)
    {
        configure(new StorageOptionsBuilder(services));
        return services;
    }

    public static StorageOptionsBuilder UseEntityFrameworkCore(this StorageOptionsBuilder builder)
    {
        builder.Services.AddScoped<ISpanReader, EntityFrameworkSpanReader>();
        builder.Services.AddScoped<ISpanWriter, EntityFrameworkSpanWriter>();
        return builder;
    }
}
