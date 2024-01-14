// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Models;
using Mocha.Core.Models.Trace;

namespace Mocha.Storage.EntityFrameworkCore.Trace;

public class EFSpan
{
    public long Id { get; init; }

    public required string TraceId { get; init; }

    public required string SpanId { get; init; }

    public required string SpanName { get; init; }

    public required string ParentSpanId { get; init; }

    public ulong StartTimeUnixNano { get; init; }

    public ulong EndTimeUnixNano { get; init; }

    public ulong DurationNanoseconds { get; init; }

    public EFSpanStatusCode? StatusCode { get; init; }

    public string? StatusMessage { get; init; }

    public EFSpanKind SpanKind { get; init; }

    public required string ServiceName { get; init; }

    public required string ServiceInstanceId { get; init; }

    public uint TraceFlags { get; init; }

    public string? TraceState { get; init; }
}
