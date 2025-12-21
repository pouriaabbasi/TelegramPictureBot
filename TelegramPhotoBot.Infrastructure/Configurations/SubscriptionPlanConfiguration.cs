using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.ValueObjects;

namespace TelegramPhotoBot.Infrastructure.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("SubscriptionPlans");

        builder.HasKey(sp => sp.Id);

        builder.Property(sp => sp.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(sp => sp.Description)
            .HasMaxLength(1000);

        // Configure TelegramStars as Value Object (Owned Entity)
        builder.OwnsOne(sp => sp.Price, price =>
        {
            price.Property(s => s.Amount)
                .HasColumnName("Price")
                .IsRequired();
        });

        builder.Property(sp => sp.DurationDays)
            .IsRequired();

        // Relationships
        builder.HasOne(sp => sp.CreatedByAdmin)
            .WithMany()
            .HasForeignKey(sp => sp.CreatedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure backing field for Subscriptions collection
        builder.Metadata.FindNavigation(nameof(SubscriptionPlan.Subscriptions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // Indexes
        builder.HasIndex(sp => sp.IsActive)
            .HasDatabaseName("IX_SubscriptionPlans_IsActive");
    }
}

