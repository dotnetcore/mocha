using Mocha.Core.Model.Trace;
using Mocha.Core.Storage;

namespace Mocha.Storage.Mysql;

public class MySqlSpanWrite:ISpanWrite
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

