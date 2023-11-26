using Microsoft.Extensions.DependencyInjection;
using Mocha.Core.Storage;

namespace Mocha.Storage.EntityFrameworkCore;

public static class EntityFrameworkCoreOptionsBuilderExtensions
{
    public static StorageOptionsBuilder UseEntityFrameworkCore(this StorageOptionsBuilder builder)
    {
        builder.Services.AddScoped<ISpanReader, EntityFrameworkSpanReader>();
        builder.Services.AddScoped<ISpanWriter, EntityFrameworkSpanWriter>();
        return builder;
    }
}
