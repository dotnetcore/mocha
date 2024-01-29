// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Storage.Jaeger.Trace;

namespace Mocha.Core.Storage.Jaeger;

public interface IJaegerSpanReader
{
    Task<IEnumerable<string>> GetServicesAsync();

    Task<IEnumerable<string>> GetOperationsAsync(string serviceName);

    Task<IEnumerable<JaegerTrace>> FindTracesAsync(JaegerTraceQueryParameters query);

    Task<IEnumerable<JaegerTrace>> FindTracesAsync(string[] traceIDs, ulong? startTimeMinUnixNano = null, ulong? startTimeMaxUnixNano = null);
}
