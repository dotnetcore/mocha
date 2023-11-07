// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Storage;
using Mocha.Core.Storage.Query;

namespace Mocha.Storage.EntityFrameworkCore;

public class EntityFrameworkSpanReader : ISpanReader
{
    private readonly MochaContext _mochaContext;

    public EntityFrameworkSpanReader(MochaContext mochaContext)
    {
        _mochaContext = mochaContext;
    }

    public Task<IEnumerable<string>> FindTraceIdListAsync(TraceReadQuery query)
    {
        throw new NotImplementedException();
    }

    public Task FindSpanListByTraceIdAsync(string traceId)
    {
        throw new NotImplementedException();
    }
}
