// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Mocha.Core.Models.Trace;
using Mocha.Core.Storage;
using Mocha.Storage.EntityFrameworkCore.Trace;

namespace Mocha.Storage.EntityFrameworkCore;

internal class EFSpanWriter(IDbContextFactory<MochaContext> factory) : ISpanWriter
{
    public async Task WriteAsync(IEnumerable<MochaSpan> spans)
    {
        var efSpans = new List<EFSpan>();
        var efSpanAttributes = new List<EFSpanAttribute>();
        var efResourceAttributes = new List<EFResourceAttribute>();
        var efSpanEvents = new List<EFSpanEvent>();
        var efSpanEventAttributes = new List<EFSpanEventAttribute>();
        var efSpanLinks = new List<EFSpanLink>();
        var efSpanLinkAttributes = new List<EFSpanLinkAttribute>();

        foreach (var span in spans)
        {
            var efSpan = span.ToEFSpan();
            efSpans.Add(efSpan);
            efSpanAttributes.AddRange(span.ToEFSpanAttributes());
            efResourceAttributes.AddRange(span.ToEFResourceAttributes());

            var spanEvents = span.Events.ToArray();
            for (var i = 0; i < spanEvents.Length; i++)
            {
                var spanEvent = spanEvents[i];
                var efSpanEvent = spanEvent.ToEFSpanEvent(span, i);
                efSpanEvents.Add(efSpanEvent);
                efSpanEventAttributes.AddRange(spanEvent.ToEFSpanEventAttributes(efSpanEvent));
            }

            var spanLinks = span.Links.ToArray();
            for (var i = 0; i < spanLinks.Length; i++)
            {
                var spanLink = spanLinks[i];
                var efSpanLink = spanLink.ToEFSpanLink(span, i);
                efSpanLinks.Add(efSpanLink);
                efSpanLinkAttributes.AddRange(spanLink.ToEFSpanLinkAttributes(efSpanLink));
            }
        }

        await using var context = await factory.CreateDbContextAsync();

        await context.Spans.AddRangeAsync(efSpans);
        await context.SpanAttributes.AddRangeAsync(efSpanAttributes);
        await context.ResourceAttributes.AddRangeAsync(efResourceAttributes);
        await context.SpanEvents.AddRangeAsync(efSpanEvents);
        await context.SpanEventAttributes.AddRangeAsync(efSpanEventAttributes);
        await context.SpanLinks.AddRangeAsync(efSpanLinks);
        await context.SpanLinkAttributes.AddRangeAsync(efSpanLinkAttributes);

        await context.SaveChangesAsync();
    }
}
