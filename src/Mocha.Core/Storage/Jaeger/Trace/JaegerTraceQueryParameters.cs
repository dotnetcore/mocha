// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Storage.Jaeger.Trace;

public class JaegerTraceQueryParameters
{
    public string? ServiceName { get; init; }

    public string? OperationName { get; init; }

    public Dictionary<string, object>? Tags { get; init; }

    public ulong? StartTimeMinUnixNano { get; init; }

    public ulong? StartTimeMaxUnixNano { get; init; }

    public ulong? DurationMinNanoseconds { get; init; }

    public ulong? DurationMaxNanoseconds { get; init; }

    public int NumTraces { get; init; }
}
