// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Storage.Jaeger.Trace;

public static class JaegerSpanKind
{
    public const string Unspecified = "unspecified";
    public const string Internal = "internal";
    public const string Server = "server";
    public const string Client = "client";
    public const string Producer = "producer";
    public const string Consumer = "consumer";
}
