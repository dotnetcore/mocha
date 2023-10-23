// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Storage.Query;

namespace Mocha.Core.Storage;

public interface ISpanReader
{
    Task<IEnumerable<string>> FindTraceIdListAsync(TraceReadQuery query);

    Task FindSpanListByTraceIdAsync(string traceId);
}
