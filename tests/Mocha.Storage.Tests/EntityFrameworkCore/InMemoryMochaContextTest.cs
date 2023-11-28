// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Mocha.Storage.EntityFrameworkCore;
using Mocha.Storage.EntityFrameworkCore.Trace;

namespace Mocha.Storage.Tests.EntityFrameworkCore;

public class InMemoryMochaContextTest
{
    private readonly DbContextOptions<MochaContext> _contextOptions;
    public InMemoryMochaContextTest()
    {
        _contextOptions = new DbContextOptionsBuilder<MochaContext>()
            .UseInMemoryDatabase("InMemoryMochaContextTest")
            .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    }

    [Fact]
    public async Task CreateDatabase()
    {
        await using var context = new MochaContext(_contextOptions);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        context.AddRange();
        await context.SaveChangesAsync();
    }


    [Fact]
    public async Task AddSpanAsync()
    {
        await using var context = new MochaContext(_contextOptions);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        context.Spans.Add(SpanBase.CreateSpan());
        await context.SaveChangesAsync();
    }
}
