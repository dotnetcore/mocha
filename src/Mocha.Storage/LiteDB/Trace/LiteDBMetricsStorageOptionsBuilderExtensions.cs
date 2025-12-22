// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Mocha.Core.Models.Trace;
using Mocha.Core.Storage;
using Mocha.Core.Storage.Jaeger;
using Mocha.Storage.LiteDB.Trace.Models;
using Mocha.Storage.LiteDB.Trace.Readers.Jaeger;
using Mocha.Storage.LiteDB.Trace.Writers;

namespace Mocha.Storage.LiteDB.Trace;

public static class LiteDBTracingStorageOptionsBuilderExtensions
{
    public static TracingStorageOptionsBuilder UseLiteDB(
        this TracingStorageOptionsBuilder builder,
        Action<LiteDBTracingOptions> configure)
    {
        builder.Services.AddOptions();
        builder.Services.Configure(configure);

        builder.Services.AddSingleton<ILiteDBCollectionAccessor<LiteDBSpan>, LiteDBSpansCollectionAccessor>();
        builder.Services.AddSingleton<ITelemetryDataWriter<MochaSpan>, LiteDBSpanWriter>();
        builder.Services.AddSingleton<IJaegerSpanReader, LiteDBJaegerSpanReader>();

        return builder;
    }
}
