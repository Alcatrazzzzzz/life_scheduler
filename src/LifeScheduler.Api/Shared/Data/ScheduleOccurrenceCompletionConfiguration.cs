using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeScheduler.Api.Shared.Data;

public sealed class ScheduleOccurrenceCompletionConfiguration
    : IEntityTypeConfiguration<ScheduleOccurrenceCompletion>
{
    public void Configure(EntityTypeBuilder<ScheduleOccurrenceCompletion> builder)
    {
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => new { c.ScheduleEntryId, c.Date }).IsUnique();
        builder
            .HasOne<ScheduleEntry>()
            .WithMany()
            .HasForeignKey(c => c.ScheduleEntryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
