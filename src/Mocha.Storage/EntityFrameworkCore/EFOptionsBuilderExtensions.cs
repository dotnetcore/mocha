// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Core.Storage;
using Mocha.Core.Storage.Jaeger;
using Mocha.Storage.EntityFrameworkCore.Jaeger;

namespace Mocha.Storage.EntityFrameworkCore;

public static class EFOptionsBuilderExtensions
{
    public static StorageOptionsBuilder UseEntityFrameworkCore(
        this StorageOptionsBuilder builder,
        Action<DbContextOptionsBuilder> optionsAction)
    {
        builder.Services.AddSingleton<ISpanWriter, EFSpanWriter>();
        builder.Services.AddSingleton<IJaegerSpanReader, EFJaegerSpanReader>();
        builder.Services.AddPooledDbContextFactory<MochaContext>(optionsAction);
        return builder;
    }
}
