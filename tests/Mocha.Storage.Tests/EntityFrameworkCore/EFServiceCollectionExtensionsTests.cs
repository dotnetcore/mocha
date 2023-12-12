// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Storage.EntityFrameworkCore;

namespace Mocha.Storage.Tests.EntityFrameworkCore;

public class EFServiceCollectionExtensionsTests
{
    [Fact]
    public void AddStorage()
    {
        var services = new ServiceCollection();
        services.AddStorage(x =>
        {
            x.UseEntityFrameworkCore(context =>
            {
                context.UseInMemoryDatabase($"InMemoryMochaContextTest{Guid.NewGuid().ToString()}")
                    .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });
        });
    }
}
