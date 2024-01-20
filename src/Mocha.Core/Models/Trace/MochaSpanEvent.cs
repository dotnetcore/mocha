// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Models.Trace;

public class MochaSpanEvent
{
    public required string Name { get; init; }

    public required IEnumerable<MochaAttribute> Attributes { get; init; }

    public ulong TimestampUnixNano { get; init; }
}
