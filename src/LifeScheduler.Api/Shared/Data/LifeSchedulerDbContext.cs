using Microsoft.EntityFrameworkCore;

namespace LifeScheduler.Api.Shared.Data;

public sealed class LifeSchedulerDbContext(DbContextOptions<LifeSchedulerDbContext> options)
    : DbContext(options)
{
    public DbSet<ScheduleEntry> Schedules => Set<ScheduleEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LifeSchedulerDbContext).Assembly);
    }
}
