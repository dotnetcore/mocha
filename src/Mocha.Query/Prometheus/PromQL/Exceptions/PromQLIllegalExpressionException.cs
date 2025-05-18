// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Query.Prometheus.PromQL.Exceptions;

internal class PromQLIllegalExpressionException : Exception
{
    public PromQLIllegalExpressionException(string message) : base(message)
    {
    }

    public PromQLIllegalExpressionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
