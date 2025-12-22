// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Storage.LiteDB;

public abstract class LiteDBOptions
{
    /// <summary>
    /// Gets or sets the directory path for the LiteDB database file.
    /// </summary>
    public required string DatabasePath { get; set; }
}
