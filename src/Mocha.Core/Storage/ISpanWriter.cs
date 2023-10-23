// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using OpenTelemetry.Proto.Trace.V1;

namespace Mocha.Core.Storage;

public interface ISpanWriter
{
    Task WriteAsync(IEnumerable<Span> spans);
}
