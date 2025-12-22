// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Models.Metadata;

public class MochaSpanMetadata
{
    public required string ServiceName { get; set; }

    public required string OperationName { get; set; }
}
