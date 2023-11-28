// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Enums;
using Mocha.Storage.EntityFrameworkCore.Trace;

namespace Mocha.Storage.Tests.EntityFrameworkCore;

public class SpanBase
{
    public static Span CreateSpan()
    {
        return new Span()
        {
            Id = 1,
            TraceId = Guid.NewGuid().ToString(),
            SpanId = Guid.NewGuid().ToString(),
            SpanName = Guid.NewGuid().ToString(),
            ParentSpanId = Guid.NewGuid().ToString(),
            ServiceName = Guid.NewGuid().ToString(),
            StartTime = DateTimeOffset.UtcNow.UtcTicks,
            EndTime = DateTimeOffset.UtcNow.UtcTicks,
            Duration = 1,
            StatusCode = 1,
            StatusMessage = Guid.NewGuid().ToString(),
            SpanKind = SpanKind.Consumer,
            TraceFlags = 1,
            TraceState = Guid.NewGuid().ToString(),
        };

    }
}
