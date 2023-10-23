// Licensed to the.NET Core Community under one or more agreements.
// The.NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Storage;

namespace Mocha.Storage.EntityFrameworkStorage;

public class EntityFrameworkSpanWriter : ISpanWriter
{
    public Task<bool> WriteAsync(IEnumerable<OpenTelemetry.Proto.Trace.V1.Span> spans)
    {
        throw new NotImplementedException();
    }
}
