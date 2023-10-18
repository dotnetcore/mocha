// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Storage;

namespace Mocha.Storage.Mysql;

public class MySqlSpanWriter:ISpanWriter
{
    public Task<bool> WriterAsync()
    {
        throw new NotImplementedException();
    }

    public bool Writer()
    {
        throw new NotImplementedException();
    }
}
