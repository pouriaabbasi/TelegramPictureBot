using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Infrastructure.Configurations;

public class ContentNotificationConfiguration : IEntityTypeConfiguration<ContentNotification>
{
    public void Configure(EntityTypeBuilder<ContentNotification> builder)
    {
        builder.ToTable("ContentNotifications");

        builder.HasKey(cn => cn.Id);

        builder.Property(cn => cn.ContentId)
            .IsRequired();

        builder.Property(cn => cn.ModelId)
            .IsRequired();

        builder.Property(cn => cn.UserId)
            .IsRequired();

        builder.Property(cn => cn.Status)
            .IsRequired();

        builder.Property(cn => cn.SentAt)
            .IsRequired(false);

        builder.Property(cn => cn.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(cn => cn.ErrorMessage)
            .IsRequired(false)
            .HasMaxLength(1000);

        // Relationships
        builder.HasOne(cn => cn.Content)
            .WithMany()
            .HasForeignKey(cn => cn.ContentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cn => cn.Model)
            .WithMany()
            .HasForeignKey(cn => cn.ModelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cn => cn.User)
            .WithMany()
            .HasForeignKey(cn => cn.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for performance
        builder.HasIndex(cn => cn.Status);
        builder.HasIndex(cn => new { cn.ModelId, cn.ContentId });
        builder.HasIndex(cn => cn.UserId);
        builder.HasIndex(cn => cn.CreatedAt);

        // Global query filter for soft delete
        builder.HasQueryFilter(cn => !cn.IsDeleted);
    }
}
