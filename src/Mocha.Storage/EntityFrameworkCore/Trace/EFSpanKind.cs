// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Storage.EntityFrameworkCore.Trace;

public enum EFSpanKind
{
    Unspecified = 0,
    Client = 1,
    Server = 2,
    Internal = 3,
    Producer = 4,
    Consumer = 5
}
