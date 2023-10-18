using Mocha.Core.Storage;

namespace Mocha.Storage.EntityFrameworkStorage;

public class EntityFrameworkSpanReader:ISpanReader
{

    public Task FindTraceList(string serviceName)
    {
        throw new NotImplementedException();
    }
}
