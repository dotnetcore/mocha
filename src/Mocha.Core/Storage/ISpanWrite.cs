using Mocha.Core.Model.Trace;

namespace Mocha.Core.Storage;

public interface ISpanWrite
{
    Task<bool> WriterAsync(Span span);

    bool Writer(Span span);
}
