// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Storage;

namespace Mocha.Storage.Mysql;

public class MySqlSpanReader:ISpanReader
{

    public Task FindTraceList(string serviceName)
    {
        throw new NotImplementedException();
    }
}
