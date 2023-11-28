// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Mocha.Core.Buffer;
using Mocha.Storage.EntityFrameworkCore;

namespace Mocha.Storage.Tests.EntityFrameworkCore;

public class EntityFrameworkCoreServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMemoryBuffer()
    {
        var services = new ServiceCollection();
        services.AddStorage(x =>
        {
            x.UseEntityFrameworkCore();
            x.Services.AddDbContext<MochaContext>();
        });

    }
}
