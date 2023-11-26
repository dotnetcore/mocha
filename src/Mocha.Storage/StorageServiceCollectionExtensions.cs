using Mocha.Storage;

namespace Microsoft.Extensions.DependencyInjection;

public static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddStorage(this IServiceCollection services, Action<StorageOptionsBuilder> configure)
    {
        configure(new StorageOptionsBuilder(services));
        return services;
    }
}
