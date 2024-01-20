// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Storage;

public class StorageOptionsBuilder(IServiceCollection services)
{
    public IServiceCollection Services { get; } = services;
}
