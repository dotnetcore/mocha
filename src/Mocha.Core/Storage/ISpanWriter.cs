namespace Mocha.Core.Storage;

public interface ISpanWriter
{
    Task<bool> WriterAsync();

    bool Writer();
}
