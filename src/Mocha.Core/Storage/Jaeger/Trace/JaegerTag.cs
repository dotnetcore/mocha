// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Storage.Jaeger.Trace;

public class JaegerTag
{
    public required string Key { get; set; }

    public required string Type { get; set; }

    public required object Value { get; set; }
}
