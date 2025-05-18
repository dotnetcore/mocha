// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Mocha.Core.Buffer;
using Mocha.Core.Models.Metrics;
using Mocha.Core.Models.Trace;
using Mocha.Distributor.Exporters;
using Mocha.Distributor.Services;
using Mocha.Storage;
using Mocha.Storage.EntityFrameworkCore;
using Mocha.Storage.EntityFrameworkCore.Metadata;
using Mocha.Storage.EntityFrameworkCore.Trace;
using Mocha.Storage.InfluxDB;

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
        bufferOptions.AddTopic<MochaSpan>("otlp-span", Environment.ProcessorCount);
        bufferOptions.AddTopic<MochaMetric>("otlp-metric", Environment.ProcessorCount);
        bufferOptions.AddTopic<MochaMetricMetadata>("otlp-metric-metadata", 1);
    });
});

builder.Services.AddStorage()
    .WithMetadata(metadataOptions =>
    {
        metadataOptions.UseEntityFrameworkCore(options =>
        {
            var connectionString = builder.Configuration.GetSection("Metadata:Storage:EF").Value;
            var serverVersion = ServerVersion.AutoDetect(connectionString);
            options.UseMySql(connectionString, serverVersion);
        });
    })
    .WithTracing(tracingOptions =>
    {
        tracingOptions.UseEntityFrameworkCore(efOptions =>
        {
            var connectionString = builder.Configuration.GetSection("Trace:Storage:EF").Value;
            var serverVersion = ServerVersion.AutoDetect(connectionString);
            efOptions.UseMySql(connectionString, serverVersion);
        });
    })
    .WithMetrics(metricsOptions =>
    {
        metricsOptions.UseInfluxDB(influxOptions =>
        {
            builder.Configuration.GetSection("Metrics:Storage:InfluxDB").Bind(influxOptions);
        });
    });

builder.Services.AddHostedService<StorageExporter>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<OTelTraceExportService>();
app.MapGrpcService<OTelMetricsExportService>();

app.MapGrpcReflectionService();

app.Run();
