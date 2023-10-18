namespace Mocha.Core.Storage;

/// <summary>
///
/// </summary>
public interface ISpanReader
{

    Task FindTraceList(string serviceName);
}
