using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Infrastructure.Configurations;

public class ModelSubscriptionConfiguration : IEntityTypeConfiguration<ModelSubscription>
{
    public void Configure(EntityTypeBuilder<ModelSubscription> builder)
    {
        // No need to call ToTable() here - TPH uses the base table
        // No need to configure key - inherited from Purchase

        // Properties
        builder.Property(ms => ms.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(ms => ms.AutoRenew)
            .IsRequired()
            .HasDefaultValue(false);

        // Owned entity: SubscriptionPeriod
        builder.OwnsOne(ms => ms.SubscriptionPeriod, period =>
        {
            period.Property(p => p.StartDate)
                .HasColumnName("SubscriptionPeriod_StartDate")
                .IsRequired();

            period.Property(p => p.EndDate)
                .HasColumnName("SubscriptionPeriod_EndDate")
                .IsRequired();
        });

        // Relationships
        builder.HasOne(ms => ms.Model)
            .WithMany(m => m.Subscriptions)
            .HasForeignKey(ms => ms.ModelId)
            .OnDelete(DeleteBehavior.Restrict);

        // Note: User relationship is inherited from Purchase base class
        // The UserId foreign key is already configured in PurchaseConfiguration
        // We just need to ensure the navigation property is properly mapped
        // This prevents EF Core from creating a shadow property UserId1

        // Indexes
        builder.HasIndex(ms => ms.UserId);

        builder.HasIndex(ms => ms.ModelId);

        builder.HasIndex(ms => new { ms.UserId, ms.ModelId, ms.IsActive })
            .HasFilter("[IsDeleted] = 0 AND [IsActive] = 1");

        // Soft delete query filter is inherited from Purchase
    }
}

