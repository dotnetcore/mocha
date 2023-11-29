// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Core.Storage;
using Mocha.Storage.EntityFrameworkCore;
using Mocha.Storage.EntityFrameworkCore.Trace;
using Span = OpenTelemetry.Proto.Trace.V1.Span;

namespace Mocha.Storage.Tests.EntityFrameworkCore;

public class InMemoryMochaContextTest
{
    private readonly DbContextOptions<MochaContext> _contextOptions;
    private readonly IServiceCollection _serviceCollection;

    public InMemoryMochaContextTest()
    {
        _serviceCollection = new ServiceCollection();
        _contextOptions = new DbContextOptionsBuilder<MochaContext>()
            .UseInMemoryDatabase("InMemoryMochaContextTest")
            .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _serviceCollection.AddStorage(x =>
        {
            x.UseEntityFrameworkCore();
            x.Services.AddDbContext<MochaContext>(context => { context.UseInMemoryDatabase($"InMemoryMochaContextTest{Guid.NewGuid().ToString()}").ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning)); });
        });
    }

    [Fact]
    public async Task CreateDatabase()
    {
        await using var context = new MochaContext(_contextOptions);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }


    [Fact]
    public async Task EntityFrameworkSpanWriterAsync()
    {
        var provider = _serviceCollection.BuildServiceProvider();
        var context = provider.GetRequiredService<MochaContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        var entityFrameworkSpanWriter = provider.GetRequiredService<ISpanWriter>();
        await entityFrameworkSpanWriter.WriteAsync(Array.Empty<Span>());
    }
}
