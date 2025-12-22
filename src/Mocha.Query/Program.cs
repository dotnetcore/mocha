using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Mocha.Query.Prometheus.Controllers;
using Mocha.Query.Prometheus.PromQL.Engine;
using Mocha.Storage;
using Mocha.Storage.EntityFrameworkCore.Metadata;
using Mocha.Storage.EntityFrameworkCore.Trace;
using Mocha.Storage.InfluxDB.Metrics;
using Mocha.Storage.LiteDB.Metadata;
using Mocha.Storage.LiteDB.Metrics;
using Mocha.Storage.LiteDB.Trace;

var builder = WebApplication.CreateSlimBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddSingleton<IPromQLParser, MochaPromQLParserParser>();
builder.Services.AddSingleton<IPromQLEngine, PromQLEngine>();
builder.Services.AddSingleton<PrometheusExceptionFilter>();

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
                throw new NotSupportedException(
                    $"Metadata storage provider '{storageProvider}' is not supported.");
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
                throw new NotSupportedException(
                    $"Trace storage provider '{storageProvider}' is not supported.");
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
                throw new NotSupportedException(
                    $"Metrics storage provider '{storageProvider}' is not supported.");
        }
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
