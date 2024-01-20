// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Query.Jaeger.DTOs;

public class JaegerResponse<T>(T data)
{
    public JaegerResponseError? Error { get; init; }
    public T Data { get; init; } = data;
}
