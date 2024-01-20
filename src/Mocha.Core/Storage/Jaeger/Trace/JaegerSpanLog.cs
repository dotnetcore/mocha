// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Storage.Jaeger.Trace;

public class JaegerSpanLog
{
    public ulong Timestamp { get; init; }

    public required JaegerTag[] Fields { get; init; }
}
