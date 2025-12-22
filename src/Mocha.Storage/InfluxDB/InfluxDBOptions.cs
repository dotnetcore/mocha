// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Storage.InfluxDB;

public class InfluxDBOptions
{
    public required string Url { get; set; }

    public required string Token { get; set; }

    public required string Org { get; set; }

    public required string Bucket { get; set; }
}
