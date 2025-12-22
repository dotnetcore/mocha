// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Mocha.Core.Buffer;
using Mocha.Core.Models.Metadata;
using Mocha.Core.Models.Metrics;
using Mocha.Core.Models.Trace;
using Mocha.Distributor.Exporters;
using Mocha.Distributor.Services;
using Mocha.Storage;
using Mocha.Storage.EntityFrameworkCore;
using Mocha.Storage.EntityFrameworkCore.Metadata;
using Mocha.Storage.EntityFrameworkCore.Trace;
using Mocha.Storage.InfluxDB;
using Mocha.Storage.InfluxDB.Metrics;
using Mocha.Storage.LiteDB.Metadata;
using Mocha.Storage.LiteDB.Metrics;
using Mocha.Storage.LiteDB.Trace;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Services.AddBuffer(options =>
{
    options.UseMemory(bufferOptions =>
    {
        bufferOptions.AddTopic<MochaSpanMetadata>("otlp-span-metadata", 1);
        bufferOptions.AddTopic<MochaMetricMetadata>("otlp-metric-metadata", 1);

        bufferOptions.AddTopic<MochaSpan>("otlp-span", Environment.ProcessorCount);
        bufferOptions.AddTopic<MochaMetric>("otlp-metric", Environment.ProcessorCount);
    });
});

builder.Services.AddStorage()
    .WithMetadata(metadataOptions =>
    {
        var storageProvider = builder.Configuration.GetValue<string>("Metadata:Storage:Provider");
        switch (storageProvider)
        {
            case MetadataStorageProvider.LiteDB:
                metadataOptions.UseLiteDB(liteDbOptions =>
                {
                    builder.Configuration.GetSection("Metadata:Storage:LiteDB").Bind(liteDbOptions);
                });
                break;
            case MetadataStorageProvider.EFCore:
                metadataOptions.UseEntityFrameworkCore(efOptions =>
                {
                    var connectionString = builder.Configuration.GetSection("Metadata:Storage:EFCore").Value;
                    var serverVersion = ServerVersion.AutoDetect(connectionString);
                    efOptions.UseMySql(connectionString, serverVersion);
                });
                break;
            default:
                throw new InvalidOperationException($"Unsupported metadata storage provider: {storageProvider}");
        }
    })
    .WithTracing(tracingOptions =>
    {
        var storageProvider = builder.Configuration.GetValue<string>("Tracing:Storage:Provider");
        switch (storageProvider)
        {
            case TracingStorageProvider.LiteDB:
                tracingOptions.UseLiteDB(liteDbOptions =>
                {
                    builder.Configuration.GetSection("Tracing:Storage:LiteDB").Bind(liteDbOptions);
                });
                break;
            case TracingStorageProvider.EFCore:
                tracingOptions.UseEntityFrameworkCore(efOptions =>
                {
                    var connectionString = builder.Configuration.GetSection("Tracing:Storage:EFCore").Value;
                    var serverVersion = ServerVersion.AutoDetect(connectionString);
                    efOptions.UseMySql(connectionString, serverVersion);
                });
                break;
            default:
                throw new InvalidOperationException($"Unsupported tracing storage provider: {storageProvider}");
        }
    })
    .WithMetrics(metricsOptions =>
    {
        var storageProvider = builder.Configuration.GetValue<MetricsStorageProvider>("Metrics:Storage:Provider");
        switch (storageProvider)
        {
            case MetricsStorageProvider.LiteDB:
                metricsOptions.UseLiteDB(liteDbOptions =>
                {
                    builder.Configuration.GetSection("Metrics:Storage:LiteDB").Bind(liteDbOptions);
                });
                break;
            case MetricsStorageProvider.InFluxDB:
                metricsOptions.UseInfluxDB(influxOptions =>
                {
                    builder.Configuration.GetSection("Metrics:Storage:InfluxDB").Bind(influxOptions);
                });
                break;
            default:
                throw new InvalidOperationException($"Unsupported metrics storage provider: {storageProvider}");
        }
    });


builder.Services.AddHostedService<StorageExporter>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<OTelTraceExportService>();
app.MapGrpcService<OTelMetricsExportService>();

app.MapGrpcReflectionService();

app.Run();
