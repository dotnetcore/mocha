// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Models.Trace;

public class MochaResource
{
    public required string ServiceName { get; init; }

    public required string ServiceInstanceId { get; init; }

    public required IEnumerable<MochaAttribute> Attributes { get; init; }
}
