using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Mocha.Query;
using Mocha.Query.Prometheus.Controllers;
using Mocha.Query.Prometheus.PromQL.Engine;
using Mocha.Storage;
using Mocha.Storage.EntityFrameworkCore.Metadata;
using Mocha.Storage.EntityFrameworkCore.Trace;
using Mocha.Storage.InfluxDB;

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

builder.Services.AddSingleton<IPromQLParser, Parser>();
builder.Services.AddSingleton<IPromQLEngine, PromQLEngine>();
builder.Services.AddSingleton<PrometheusExceptionFilter>();

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
