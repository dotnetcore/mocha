namespace Mocha.Storage.Tests.LiteDB;

public class TempDatabasePath : IDisposable
{
    private TempDatabasePath(string path)
    {
        Path = path;
    }

    public string Path { get; }

    public void Dispose()
    {
        if (!Directory.Exists(Path))
        {
            return;
        }

        try
        {
            Directory.Delete(Path, true);
        }
        catch (IOException)
        {
            // Ignore IO exceptions during cleanup
        }
    }

    public static TempDatabasePath Create()
    {
        var path = "temp_" + Guid.NewGuid().ToString("N");
        return new TempDatabasePath(path);
    }
}
