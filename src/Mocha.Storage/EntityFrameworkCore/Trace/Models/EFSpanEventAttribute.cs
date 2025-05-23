// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Storage.EntityFrameworkCore.Trace.Models;

public class EFSpanEventAttribute : AbstractEFAttribute
{
    public long Id { get; init; }

    public required string TraceId { get; init; }

    public required string SpanId { get; init; }

    public int SpanEventIndex { get; init; }
}
