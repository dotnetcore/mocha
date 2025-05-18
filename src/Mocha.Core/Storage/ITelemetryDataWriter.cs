// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Storage;

public interface ITelemetryDataWriter<in T>
{
    Task WriteAsync(IEnumerable<T> data);
}
