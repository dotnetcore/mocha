// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Storage;
using Mocha.Storage.EntityFrameworkCore.Trace;

namespace Mocha.Storage.EntityFrameworkCore;

public class EntityFrameworkSpanWriter : ISpanWriter
{
    private readonly MochaContext _mochaContext;

    private readonly IConverterToEntityFrameworkStorageModel _converterToEntityFrameworkStorageModel;

    public EntityFrameworkSpanWriter(MochaContext mochaContext, IConverterToEntityFrameworkStorageModel converterToEntityFrameworkStorageModel)
    {
        _mochaContext = mochaContext;
        _converterToEntityFrameworkStorageModel = converterToEntityFrameworkStorageModel;
    }

    public async Task WriteAsync(IEnumerable<OpenTelemetry.Proto.Trace.V1.Span> spans)
    {
        var entityFrameworkSpans = spans.Select(_converterToEntityFrameworkStorageModel.ConverterToSpan);
        _mochaContext.Spans.AddRange(entityFrameworkSpans);
        await _mochaContext.SaveChangesAsync();
    }


    private SpanAttribute StructureSpanAttribute()
    {
        return new SpanAttribute() { };
    }

    private SpanAttribute StructureSpanLink()
    {
        return new SpanAttribute() { };
    }

    private SpanEvent StructureSpanEvent()
    {
        return new SpanEvent() { };
    }
}
