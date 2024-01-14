// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Storage.Jaeger;

public class JaegerTraceQueryParameters
{
    public string? ServiceName { get; set; }

    public string? OperationName { get; set; }

    public Dictionary<string, object>? Tags { get; set; }

    public ulong? StartTimeMinUnixNano { get; set; }

    public ulong? StartTimeMaxUnixNano { get; set; }

    public ulong? DurationMinNanoseconds { get; set; }

    public ulong? DurationMaxNanoseconds { get; set; }

    public int NumTraces { get; set; }
}
