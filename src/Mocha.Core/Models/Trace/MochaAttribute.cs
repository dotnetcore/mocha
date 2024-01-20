// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Models.Trace;

public class MochaAttribute
{
    public required string Key { get; init; }

    public MochaAttributeValueType ValueType { get; init; }

    public required string Value { get; init; }
}
