// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Storage;

namespace Mocha.Storage.EntityFrameworkCore;

public class EFSpanWriter : ISpanWriter
{
    private readonly MochaContext _mochaContext;

    public EFSpanWriter(MochaContext mochaContext)
    {
        _mochaContext = mochaContext;
    }

    public async Task WriteAsync(IEnumerable<OpenTelemetry.Proto.Trace.V1.Span> spans)
    {
        var entityFrameworkSpans = spans.Select(span => span.ToEFSpan());
        _mochaContext.Spans.AddRange(entityFrameworkSpans);
        await _mochaContext.SaveChangesAsync();
    }
}
