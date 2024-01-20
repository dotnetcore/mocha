// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Storage.EntityFrameworkCore.Trace;

public abstract class AbstractEFAttribute
{
    public required string Key { get; init; }

    public required EFAttributeValueType ValueType { get; init; }

    public required string Value { get; init; }
}
