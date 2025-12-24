using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Infrastructure.Configurations;

public class PurchaseSubscriptionConfiguration : IEntityTypeConfiguration<PurchaseSubscription>
{
    public void Configure(EntityTypeBuilder<PurchaseSubscription> builder)
    {
        // TPH - All purchase types share the same table (Purchases)
        // No ToTable() call needed

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

