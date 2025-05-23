// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.ComponentModel;

namespace Mocha.Query.Prometheus.DTOs;

public enum ErrorType
{
    [Description("bad_data")]
    BadData,

    [Description("internal")]
    Internal
}
