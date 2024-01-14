// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Query.Jaeger.DTOs;

public class JaegerResponseError
{
    public int Code { get; init; }
    public required string Message { get; init; }
}
