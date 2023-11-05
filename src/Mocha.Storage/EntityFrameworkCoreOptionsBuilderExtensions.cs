// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Storage;

public static class EntityFrameworkCoreOptionsBuilderExtensions
{
    public static IServiceCollection AddEntityFrameworkCoreStorage(
        this IServiceCollection services,Action<DbContextOptionsBuilder>? optionsAction=null)
    {
        services.AddDbContext<MochaContext>(optionsAction);
        return services;
    }
}
