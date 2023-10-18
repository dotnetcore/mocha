using Mocha.Core.Model.Trace;
using Mocha.Core.Storage;

namespace Mocha.Storage.EntityFrameworkStorage;

public class EntityFrameworkSpanWriter:ISpanWriter
{
    public Task<bool> WriteAsync(Span span)
    {
        throw new NotImplementedException();
    }

    public bool Write(Span span)
    {
        throw new NotImplementedException();
    }
}

