using Mocha.Core.Model.Trace;

namespace Mocha.Core.Storage;

public interface ISpanWriter
{
    Task<bool> WriteAsync(Span span);

    bool Write(Span span);
}
