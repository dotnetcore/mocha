// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Globalization;
using LiteDB;

namespace Mocha.Storage.LiteDB;

public static class LiteDBUtils
{
    public static ILiteDatabase OpenDatabase(string path)
    {
        EnsureDatabaseFileExists(path);
        var connectionString = new ConnectionString
        {
            Filename = path,
            Connection = ConnectionType.Shared,
            Collation = new Collation("/IgnoreCase")
        };
        return new LiteDatabase(connectionString);
    }

    private static void EnsureDatabaseFileExists(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(path))
        {
            using var fs = File.Create(path);
        }
    }
}
