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


}
