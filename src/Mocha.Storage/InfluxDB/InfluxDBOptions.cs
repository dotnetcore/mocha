// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Storage.InfluxDB;

public class InfluxDBOptions
{
    public string? Url { get; set; }

    public string? Token { get; set; }

    public string? Org { get; set; }

    public string? Bucket { get; set; }
}
