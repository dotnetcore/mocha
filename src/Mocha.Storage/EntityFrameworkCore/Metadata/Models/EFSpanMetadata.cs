// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Storage.EntityFrameworkCore.Metadata.Models;

public class EFSpanMetadata
{
    public long Id { get; init; }

    public required string ServiceName { get; init; }

    public required string OperationName { get; init; }
}
