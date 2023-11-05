using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddStorage(options =>
{
    options.Services.AddDbContext<MochaContext>(dbContext =>
    {
        dbContext.UseInMemoryDatabase("db");
    });
});
