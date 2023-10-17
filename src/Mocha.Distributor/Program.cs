using System.Net;
using Mocha.Distributor.Services;

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

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<OTelTraceExportService>();

app.MapGrpcReflectionService();

app.Run();
