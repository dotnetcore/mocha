// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Query.Prometheus.DTOs;

/// <summary>
/// Mock build info for PromQL API, sometimes the client(such as Grafana) use it to detect the version of the data-source server
/// for different API support.
/// </summary>
public class BuildInfo
{
    public string? Version { get; set; }

    public string? Revision { get; set; }

    public string? Branch { get; set; }

    public string? BuildUser { get; set; }

    public string? BuildDate { get; set; }

    public string? GoVersion { get; set; }
}
