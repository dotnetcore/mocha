// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Mocha.Query.JsonConverters;
using Mocha.Query.Prometheus.PromQL.Values;

namespace Mocha.Query.Prometheus.DTOs;

[JsonPolymorphic]
[JsonDerivedType(typeof(MatrixRspData))]
public class ResponseData
{
    [JsonConverter(typeof(JsonDescriptionEnumConverter<ResultValueType>))]
    public ResultValueType ResultType { get; set; }
}
