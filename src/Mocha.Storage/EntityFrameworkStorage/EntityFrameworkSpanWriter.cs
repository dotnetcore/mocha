using Mocha.Core.Storage;

namespace Mocha.Storage.EntityFrameworkStorage;

public class EntityFrameworkSpanWriter : ISpanWriter
{
    public Task<bool> WriteAsync(IEnumerable<OpenTelemetry.Proto.Trace.V1.Span> span)
    {
        throw new NotImplementedException();
    }
}
