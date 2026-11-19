using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeScheduler.Api.Shared.Data;

public sealed class ScheduleEntryConfiguration : IEntityTypeConfiguration<ScheduleEntry>
{
    public void Configure(EntityTypeBuilder<ScheduleEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Notes).HasMaxLength(2000);
        builder
            .Property(e => e.Recurrence)
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasDefaultValue(ScheduleRecurrence.None);
        builder
            .Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasDefaultValue(ScheduleStatus.Active);
        builder.HasIndex(e => e.Date);
    }
}
