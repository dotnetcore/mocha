// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using LiteDB;

namespace Mocha.Storage.LiteDB;

public abstract class LiteDBCollectionAccessor<T> : ILiteDBCollectionAccessor<T>, IDisposable
{
    private readonly ILiteDatabase _db;

    protected LiteDBCollectionAccessor(
        string databasePath,
        string collectionName)
    {
        _db = OpenDatabase(databasePath);
        Collection = _db.GetCollection<T>(collectionName);

        ConfigureCollection(Collection);
    }

    public ILiteCollection<T> Collection { get; }

    public void Dispose()
    {
        _db.Dispose();
    }

    protected abstract void ConfigureCollection(ILiteCollection<T> collection);

    private static ILiteDatabase OpenDatabase(string path)
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
