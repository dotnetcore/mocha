// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Storage.Jaeger;

public partial interface IJaegerSpanMetadataReader
{
    Task<IEnumerable<string>> GetServicesAsync();

    Task<IEnumerable<string>> GetOperationsAsync(string serviceName);
}
