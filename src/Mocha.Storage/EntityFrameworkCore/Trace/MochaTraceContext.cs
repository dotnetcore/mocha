// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Mocha.Storage.EntityFrameworkCore.Metadata.Models;
using Mocha.Storage.EntityFrameworkCore.Trace.Configurations;
using Mocha.Storage.EntityFrameworkCore.Trace.Models;

namespace Mocha.Storage.EntityFrameworkCore.Trace;

public class MochaTraceContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<EFSpan> Spans => Set<EFSpan>();

    public DbSet<EFSpanEvent> SpanEvents => Set<EFSpanEvent>();

    public DbSet<EFSpanLink> SpanLinks => Set<EFSpanLink>();

    public DbSet<EFSpanAttribute> SpanAttributes => Set<EFSpanAttribute>();

    public DbSet<EFResourceAttribute> ResourceAttributes => Set<EFResourceAttribute>();

    public DbSet<EFSpanEventAttribute> SpanEventAttributes => Set<EFSpanEventAttribute>();

    public DbSet<EFSpanLinkAttribute> SpanLinkAttributes => Set<EFSpanLinkAttribute>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new ResourceAttributeConfiguration());
        modelBuilder.ApplyConfiguration(new SpanAttributeConfiguration());
        modelBuilder.ApplyConfiguration(new SpanConfiguration());
        modelBuilder.ApplyConfiguration(new SpanEventAttributeConfiguration());
        modelBuilder.ApplyConfiguration(new SpanEventConfiguration());
        modelBuilder.ApplyConfiguration(new SpanLinkAttributeConfiguration());
        modelBuilder.ApplyConfiguration(new SpanLinkConfiguration());
    }
}
