// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using LiteDB;

namespace Mocha.Storage.LiteDB.Trace.Models;

public class LiteDBAttribute
{
    [BsonField("k")]
    public required string Key { get; init; }

    [BsonField("t")]
    public LiteDBAttributeValueType ValueType { get; init; }

    [BsonField("v")]
    public required string Value { get; init; }
}
