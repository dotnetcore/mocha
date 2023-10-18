// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Storage;

/// <summary>
///
/// </summary>
public interface ISpanReader
{

    Task FindTraceList(string serviceName);
}
