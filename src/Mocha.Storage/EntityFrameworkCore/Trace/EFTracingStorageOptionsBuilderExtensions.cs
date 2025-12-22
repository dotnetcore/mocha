// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Core.Models.Trace;
using Mocha.Core.Storage;
using Mocha.Core.Storage.Jaeger;
using Mocha.Storage.EntityFrameworkCore.Trace.Readers.Jaeger;
using Mocha.Storage.EntityFrameworkCore.Trace.Writers;

namespace Mocha.Storage.EntityFrameworkCore.Trace;

public static class EFTracingStorageOptionsBuilderExtensions
{
    public static TracingStorageOptionsBuilder UseEntityFrameworkCore(
        this TracingStorageOptionsBuilder builder,
        Action<DbContextOptionsBuilder> configure)
    {
        builder.Services.AddSingleton<ITelemetryDataWriter<MochaSpan>, EFSpanWriter>();
        builder.Services.AddSingleton<IJaegerSpanReader, EFJaegerSpanReader>();
        builder.Services.AddPooledDbContextFactory<MochaTraceContext>(configure);
        return builder;
    }
}
