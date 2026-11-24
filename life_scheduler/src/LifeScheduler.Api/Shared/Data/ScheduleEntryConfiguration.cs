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
        builder.Property(e => e.Reward).HasMaxLength(200);
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
        builder
            .Property(e => e.Kind)
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasDefaultValue(ScheduleKind.Event);
        builder.HasIndex(e => e.Date);
        builder.HasIndex(e => e.StackAfterId);
        builder
            .HasOne<ScheduleEntry>()
            .WithMany()
            .HasForeignKey(e => e.StackAfterId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
