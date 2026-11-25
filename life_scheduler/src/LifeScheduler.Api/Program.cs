using System.Text.Json;
using System.Text.Json.Serialization;
using LifeScheduler.Api.Features.Attachments;
using LifeScheduler.Api.Features.Completion;
using LifeScheduler.Api.Features.Habits;
using LifeScheduler.Api.Features.History;
using LifeScheduler.Api.Features.Schedule;
using LifeScheduler.Api.Features.VoiceAssistant;
using LifeScheduler.Api.Shared.Data;
using LifeScheduler.Api.Shared.LocalAi;
using LifeScheduler.Api.Shared.Storage;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

var connectionString = builder.Configuration.GetConnectionString("LifeSchedulerDb");
builder.Services.AddDbContext<LifeSchedulerDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddFileStorage(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddLocalAi(builder.Configuration);
builder.Services.AddScoped<PastEntryArchiver>();
builder.Services.AddHostedService<HistoryArchiverService>();

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
app.MapAttachmentEndpoints();
app.MapHistoryEndpoints();
app.MapCompletionEndpoints();
app.MapHabitEndpoints();
app.MapVoiceAssistantEndpoints();

if (app.Environment.IsDevelopment() && app.Configuration.UsesLocalStorage())
    app.MapLocalStorageEndpoints();

try
{
    LocalAiStartup.Initialize(app.Services);
}
catch (Exception ex)
{
    app.Logger.LogCritical(
        ex,
        "Локальные AI-модели не загрузились. Положите ggml-medium.bin и папку " +
        "phi-4-mini-onnx-cpu-int4 в src/LifeScheduler.Api/models/ и см. README.md.");
    Environment.ExitCode = 1;
    return;
}

app.Run();

public partial class Program;
