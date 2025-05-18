// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Query.Prometheus.PromQL.Exceptions;

internal class PromQLBadRequestException : Exception
{
    public PromQLBadRequestException(string message) : base(message)
    {
    }

    public PromQLBadRequestException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
