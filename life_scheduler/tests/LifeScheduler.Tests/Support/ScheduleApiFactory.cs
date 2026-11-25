using System.Data;
using LifeScheduler.Api.Features.History;
using LifeScheduler.Api.Shared.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace LifeScheduler.Tests.Support;

public sealed class ScheduleApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly TimeProvider _timeProvider;

    public ScheduleApiFactory(DateTimeOffset? utcNow = null)
    {
        _timeProvider = new FixedTimeProvider(utcNow ?? DateTimeOffset.UtcNow);
        StorageRoot = Path.Combine(
            Path.GetTempPath(),
            "life-scheduler-tests",
            Guid.NewGuid().ToString("N"));
    }

    public string StorageRoot { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        builder.UseEnvironment("Development");
        builder.UseSetting("LocalAi:Enabled", "false");
        builder.UseSetting("Storage:Provider", "Local");
        builder.UseSetting("Storage:Local:RootPath", StorageRoot);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<LifeSchedulerDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<LifeSchedulerDbContext>>();
            services.RemoveAll<LifeSchedulerDbContext>();
            services.RemoveAll<TimeProvider>();
            services.RemoveAll<IHostedService>();

            services.AddDbContext<LifeSchedulerDbContext>(options => options.UseSqlite(_connection));
            services.AddSingleton(_timeProvider);
        });
    }

    public void InitializeDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LifeSchedulerDbContext>();
        db.Database.EnsureCreated();
    }

    public async Task ArchivePastEntriesAsync()
    {
        using var scope = Services.CreateScope();
        var archiver = scope.ServiceProvider.GetRequiredService<PastEntryArchiver>();
        await archiver.ArchiveAsync(CancellationToken.None);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _connection.Dispose();

        try
        {
            if (Directory.Exists(StorageRoot))
                Directory.Delete(StorageRoot, recursive: true);
        }
        catch (IOException)
        {
            // Временная папка теста — не повод валить весь прогон.
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
