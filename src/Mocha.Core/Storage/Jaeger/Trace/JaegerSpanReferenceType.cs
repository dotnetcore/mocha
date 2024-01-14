// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Core.Storage.Jaeger.Trace;

public static class JaegerSpanReferenceType
{
    public const string ChildOf = "CHILD_OF";
    public const string FollowsFrom = "FOLLOWS_FROM";
}
