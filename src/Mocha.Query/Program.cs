using Microsoft.EntityFrameworkCore;
using Mocha.Storage;
using Mocha.Storage.EntityFrameworkCore;

var builder = WebApplication.CreateSlimBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers();

builder.Services.AddStorage(options =>
{
    options.UseEntityFrameworkCore(efOptions =>
    {
        var connectionString = builder.Configuration.GetConnectionString("EF");
        var serverVersion = ServerVersion.AutoDetect(connectionString);
        efOptions.UseMySql(connectionString, serverVersion);
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
