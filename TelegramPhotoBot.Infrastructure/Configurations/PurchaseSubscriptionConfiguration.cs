using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Infrastructure.Configurations;

public class PurchaseSubscriptionConfiguration : IEntityTypeConfiguration<PurchaseSubscription>
{
    public void Configure(EntityTypeBuilder<PurchaseSubscription> builder)
    {
        // Table Per Type (TPT) - جدول جداگانه برای PurchaseSubscription
        builder.ToTable("PurchaseSubscriptions");

        // Relationships specific to PurchaseSubscription
        builder.HasOne(ps => ps.Subscription)
            .WithMany()
            .HasForeignKey(ps => ps.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(ps => ps.SubscriptionId)
            .HasDatabaseName("IX_PurchaseSubscription_SubscriptionId");
    }
}

