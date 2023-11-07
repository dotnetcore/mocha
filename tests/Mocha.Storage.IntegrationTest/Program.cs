using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Storage;
using Mocha.Storage.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddStorage(options =>
{
    options.Services.AddDbContext<MochaContext>(dbContext =>
    {
        dbContext.UseInMemoryDatabase("db");
    });
    options.UseEntityFrameworkCore();
});
