﻿// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

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