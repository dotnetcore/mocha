using Mocha.Core.Storage;

namespace Mocha.Storage.Mysql;

public class MySqlSpanReader:ISpanReader
{

    public Task FindTraceList(string serviceName)
    {
        throw new NotImplementedException();
    }
}
