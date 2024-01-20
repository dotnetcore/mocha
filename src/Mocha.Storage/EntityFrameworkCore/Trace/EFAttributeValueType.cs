// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Storage.EntityFrameworkCore.Trace;

public enum EFAttributeValueType
{
    StringValue = 1,
    BoolValue = 2,
    IntValue = 3,
    DoubleValue = 4,
    ArrayValue = 5,
    KvlistValue = 6,
    BytesValue = 7,
}
