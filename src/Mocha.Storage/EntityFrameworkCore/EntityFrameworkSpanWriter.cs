// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Diagnostics;
using Mocha.Core.Enums;
using Mocha.Core.Storage;
using Mocha.Storage.EntityFrameworkCore.Trace;

namespace Mocha.Storage.EntityFrameworkCore;

public class EntityFrameworkSpanWriter : ISpanWriter
{
    private readonly MochaContext _mochaContext;

    public EntityFrameworkSpanWriter(MochaContext mochaContext)
    {
        _mochaContext = mochaContext;
    }

    public async Task WriteAsync(IEnumerable<OpenTelemetry.Proto.Trace.V1.Span> spans)
    {
        //_mochaContext.Spans.Add()
        throw new NotImplementedException();
    }

    private Span StructureSpan(OpenTelemetry.Proto.Trace.V1.Span span)
    {
        return new Span()
        {
            SpanId = span.SpanId.ToString() ?? "",
            TraceFlags = span.Flags,
            TraceId = span.TraceId.ToString() ?? "",
            SpanName = span.Name,
            ParentSpanId = span.ParentSpanId.ToString() ?? "",
            StartTime = (long)span.StartTimeUnixNano,
            EndTime = (long)span.EndTimeUnixNano,
            //Duration = (long)span.EndTimeUnixNano - span.StartTimeUnixNano,
            StatusCode = (int)span.Status.Code,
            StatusMessage = span.Status.Message,
            TraceState = span.TraceState,
            SpanKind = GetSpanKind(span.Kind),
        };
    }


    private static SpanKind GetSpanKind(OpenTelemetry.Proto.Trace.V1.Span.Types.SpanKind spanKind)
    {
        return spanKind switch
        {
            OpenTelemetry.Proto.Trace.V1.Span.Types.SpanKind.Unspecified => SpanKind.Unspecified,
            OpenTelemetry.Proto.Trace.V1.Span.Types.SpanKind.Internal => SpanKind.Internal,
            OpenTelemetry.Proto.Trace.V1.Span.Types.SpanKind.Server => SpanKind.Server,
            OpenTelemetry.Proto.Trace.V1.Span.Types.SpanKind.Client => SpanKind.Client,
            OpenTelemetry.Proto.Trace.V1.Span.Types.SpanKind.Producer => SpanKind.Producer,
            OpenTelemetry.Proto.Trace.V1.Span.Types.SpanKind.Consumer => SpanKind.Consumer,
            _ => throw new ArgumentOutOfRangeException(nameof(spanKind), spanKind, null)
        };
    }
}
