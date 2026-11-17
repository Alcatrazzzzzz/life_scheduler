using System.Text.Json;
using System.Text.Json.Serialization;
using LifeScheduler.Api.Features.Schedule;
using LifeScheduler.Api.Shared.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

var connectionString = builder.Configuration.GetConnectionString("LifeSchedulerDb");
builder.Services.AddDbContext<LifeSchedulerDbContext>(options => options.UseSqlite(connectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "App_Data"));
}
else
{
    app.UseHttpsRedirection();
}

app.MapScheduleEndpoints();

app.Run();

public partial class Program;
