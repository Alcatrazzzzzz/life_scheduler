namespace LifeScheduler.Api.Features.History;

/// <summary>Раз в минуту переводит прошедшие записи в историю; первый прогон — при старте.</summary>
public sealed class HistoryArchiverService(
    IServiceScopeFactory scopeFactory,
    ILogger<HistoryArchiverService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunAsync(stoppingToken);

        using var timer = new PeriodicTimer(Interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await RunAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Штатная остановка приложения.
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var archiver = scope.ServiceProvider.GetRequiredService<PastEntryArchiver>();
            var archived = await archiver.ArchiveAsync(ct);

            if (archived > 0)
                logger.LogInformation("В историю переведено записей: {Count}", archived);
        }
        catch (OperationCanceledException)
        {
            // Штатная остановка приложения.
        }
        catch (Exception exception)
        {
            // Без миграции или временно недоступная БД не должны ронять приложение.
            logger.LogError(exception, "Не удалось перевести прошедшие записи в историю.");
        }
    }
}
