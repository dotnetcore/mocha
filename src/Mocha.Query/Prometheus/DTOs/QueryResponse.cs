// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Mocha.Query.JsonConverters;

namespace Mocha.Query.Prometheus.DTOs;

public class QueryResponse<T>
{
    [JsonConverter(typeof(JsonDescriptionEnumConverter<ResultStatus>))]
    public required ResultStatus Status { get; init; }

    [JsonConverter(typeof(JsonDescriptionEnumConverter<ErrorType>))]
    public ErrorType? ErrorType { get; init; }

    public string? Error { get; init; }

    public string? Warnings { get; init; }

    public T? Data { get; init; }
}
