using Mocha.Core.Enums;

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
        context.Spans.Add(CreateSpan());
        var result = await context.SaveChangesAsync();
        Assert.True(result > 0);

    }

    private static Span CreateSpan()
    {
        return new Span()
        {
            TraceId = Guid.NewGuid().ToString(),
            SpanId = Guid.NewGuid().ToString(),
            SpanName = Guid.NewGuid().ToString(),
            ParentSpanId = Guid.NewGuid().ToString(),
            ServiceName = Guid.NewGuid().ToString(),
            StartTime = DateTimeOffset.UtcNow.Ticks,
            EndTime = DateTimeOffset.UtcNow.Ticks,
            Duration = 0.02,
            StatusCode = 1,
            StatusMessage = Guid.NewGuid().ToString(),
            SpanKind = SpanKind.Client,
            TraceFlags = 1,
            TraceState = Guid.NewGuid().ToString(),
        };
    }
}
