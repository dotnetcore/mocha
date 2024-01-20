// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Net;
using Microsoft.EntityFrameworkCore;
using Mocha.Core.Buffer;
using Mocha.Core.Models.Trace;
using Mocha.Distributor.Exporters;
using Mocha.Distributor.Services;
using Mocha.Storage;
using Mocha.Storage.EntityFrameworkCore;

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
    });
});

builder.Services.AddStorage(options =>
{
    options.UseEntityFrameworkCore(efOptions =>
    {
        var connectionString = builder.Configuration.GetConnectionString("EF");
        var serverVersion = ServerVersion.AutoDetect(connectionString);
        efOptions.UseMySql(connectionString, serverVersion);
    });
});

builder.Services.AddHostedService<StorageExporter>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<OTelTraceExportService>();

app.MapGrpcReflectionService();

app.Run();
