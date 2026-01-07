using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Infrastructure.Configurations;

public class PendingStarPaymentConfiguration : IEntityTypeConfiguration<PendingStarPayment>
{
    public void Configure(EntityTypeBuilder<PendingStarPayment> builder)
    {
        builder.ToTable("PendingStarPayments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.Property(p => p.TelegramUserId)
            .IsRequired();

        builder.Property(p => p.ContentId)
            .IsRequired();

        builder.Property(p => p.ContentType)
            .IsRequired();

        builder.Property(p => p.RequiredStars)
            .IsRequired();

        builder.Property(p => p.ReceivedStars)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.PaymentMessageId)
            .IsRequired();

        builder.Property(p => p.ChatId)
            .IsRequired();

        builder.Property(p => p.Status)
            .IsRequired();

        builder.Property(p => p.ExpiresAt)
            .IsRequired();

        builder.Property(p => p.CompletedAt)
            .IsRequired(false);

        // Relationships
        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional Photo relationship (only for Photo purchases, not for Subscriptions)
        builder.HasOne(p => p.Photo)
            .WithMany()
            .HasForeignKey(p => p.ContentId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);  // Make it optional

        // Indexes for performance
        builder.HasIndex(p => new { p.PaymentMessageId, p.ChatId })
            .IsUnique();
        
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.ExpiresAt);

        // Global query filter for soft delete
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
