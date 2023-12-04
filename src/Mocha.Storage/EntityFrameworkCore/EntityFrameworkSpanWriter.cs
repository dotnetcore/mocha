// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Storage;
using Mocha.Storage.EntityFrameworkCore.Trace;

namespace Mocha.Storage.EntityFrameworkCore;

public class EntityFrameworkSpanWriter : ISpanWriter
{
    private readonly MochaContext _mochaContext;

    private readonly OTelConverter _converter;

    public EntityFrameworkSpanWriter(MochaContext mochaContext, OTelConverter converter)
    {
        _mochaContext = mochaContext;
        _converter = converter;
    }

    public async Task WriteAsync(IEnumerable<OpenTelemetry.Proto.Trace.V1.Span> spans)
    {
        var entityFrameworkSpans = spans.Select(_converter.OTelSpanToEntityFrameworkSpan);
        _mochaContext.Spans.AddRange(entityFrameworkSpans);
        await _mochaContext.SaveChangesAsync();
    }
}
