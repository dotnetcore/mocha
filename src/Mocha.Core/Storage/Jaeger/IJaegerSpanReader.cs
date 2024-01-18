// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Storage.Jaeger.Trace;

namespace Mocha.Core.Storage.Jaeger;

public interface IJaegerSpanReader
{
    Task<string[]> GetServicesAsync();

    Task<string[]> GetOperationsAsync(string serviceName);

    Task<JaegerTrace[]> FindTracesAsync(JaegerTraceQueryParameters query);

    Task<JaegerTrace[]> FindTracesAsync(string[] traceIDs, ulong? startTimeMinUnixNano = null, ulong? startTimeMaxUnixNano = null);
}
