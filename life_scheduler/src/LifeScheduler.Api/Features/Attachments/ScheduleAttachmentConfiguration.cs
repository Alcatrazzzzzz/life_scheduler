using LifeScheduler.Api.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LifeScheduler.Api.Features.Attachments;

public sealed class ScheduleAttachmentConfiguration : IEntityTypeConfiguration<ScheduleAttachment>
{
    public void Configure(EntityTypeBuilder<ScheduleAttachment> builder)
    {
        builder.ToTable("ScheduleAttachments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.FileName).IsRequired().HasMaxLength(255);
        builder.Property(a => a.ContentType).IsRequired().HasMaxLength(127);
        builder.Property(a => a.StorageKey).IsRequired().HasMaxLength(512);
        builder.HasIndex(a => a.StorageKey).IsUnique();
        builder.HasIndex(a => a.ScheduleEntryId);
        builder
            .Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasDefaultValue(AttachmentStatus.Pending);
        builder
            .HasOne<ScheduleEntry>()
            .WithMany()
            .HasForeignKey(a => a.ScheduleEntryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
