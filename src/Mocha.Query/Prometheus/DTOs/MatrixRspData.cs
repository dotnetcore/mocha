// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Mocha.Query.Prometheus.DTOs;

public class MatrixRspData : ResponseData
{
    public List<MatrixDataDTO>? Result { get; set; }
}
