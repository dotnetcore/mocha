// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Models.Trace;

public class MochaSpanLink
{
    public long Id { get; init; }

    public required string LinkedTraceId { get; init; }

    public required string LinkedSpanId { get; init; }

    public required IEnumerable<MochaAttribute> Attributes { get; init; }

    public required string LinkedTraceState { get; init; }

    public uint LinkedTraceFlags { get; init; }
}
