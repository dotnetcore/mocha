// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Net;
using Mocha.Core.Buffer;
using Mocha.Distributor.Services;
using OpenTelemetry.Proto.Trace.V1;

var builder = WebApplication.CreateBuilder(args);

// builder.Configuration.AddEnvironmentVariables(prefix: "Mocha");

builder.WebHost.ConfigureKestrel(options =>
{
    var port = builder.Configuration.GetValue<int>("OTel:Grpc:Server:Port");
    options.Listen(IPAddress.Any, port);
});

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Services.AddBuffer(options =>
{
    options.UseMemory(bufferOptions =>
    {
        bufferOptions.AddTopic<Span>("otlp_spans", Environment.ProcessorCount);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<OTelTraceExportService>();

app.MapGrpcReflectionService();

app.Run();
