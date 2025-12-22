// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using LiteDB;

namespace Mocha.Storage.LiteDB.Metadata.Models;

public class LiteDBSpanMetadata
{
    [BsonId]
    public ObjectId? Id { get; set; }

    [BsonField("s")]
    public required string ServiceName { get; set; }

    [BsonField("o")]
    public required string OperationName { get; set; }
}
