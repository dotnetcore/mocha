// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Mocha.Storage;
using Mocha.Storage.EntityFrameworkCore.Trace;

namespace Mocha.Core.Tests.MochaContextTests;

public class InMemoryMochaContextTest
{
    private readonly DbContextOptions<MochaContext> _contextOptions;
    public InMemoryMochaContextTest()
    {
        _contextOptions = new DbContextOptionsBuilder<MochaContext>()
            .UseInMemoryDatabase("InMemoryMochaContextTest")
            .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        using var context = new MochaContext(_contextOptions);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        context.AddRange();
        context.SaveChanges();
    }

    [Fact]
    public async Task AddSpan()
    {
        await using var context = new MochaContext(_contextOptions);
        context.Spans.Add(new Span());
        var result= await  context.SaveChangesAsync();
    }

}
