// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Mocha.Storage.EntityFrameworkCore.Trace;

namespace Mocha.Storage.EntityFrameworkCore;

public class MochaContext : DbContext
{
    public MochaContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<EFSpanAttribute> SpanAttributes => Set<EFSpanAttribute>();

    public DbSet<EFSpanEvent> SpanEvents => Set<EFSpanEvent>();

    public DbSet<EFSpanLink> SpanLinks => Set<EFSpanLink>();

    public DbSet<EFSpan> Spans => Set<EFSpan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
