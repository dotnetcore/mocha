namespace Mocha.Core.Storage;

public interface ISpanReader
{

    Task FindTraceList(string serviceName);
}
