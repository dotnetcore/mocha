using Mocha.Core.Model.Trace;
using Mocha.Core.Storage;

namespace Mocha.Storage.EntityFrameworkStorage;

public class EntityFrameworkSpanWrite:ISpanWrite
{
    public Task<bool> WriterAsync(Span span)
    {
        throw new NotImplementedException();
    }

    public bool Writer(Span span)
    {
        throw new NotImplementedException();
    }
}

