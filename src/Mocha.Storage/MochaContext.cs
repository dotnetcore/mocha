// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Mocha.Storage.EntityFrameworkStorage.Trace;

namespace Mocha.Storage;

public class MochaContext : DbContext
{
    public MochaContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<SpanAttribute> SpanAttributes => Set<SpanAttribute>();

    public DbSet<SpanEvent> SpanEvents => Set<SpanEvent>();

    public DbSet<SpanLink> SpanLinks => Set<SpanLink>();

    public DbSet<Span> Spans => Set<Span>();
}
