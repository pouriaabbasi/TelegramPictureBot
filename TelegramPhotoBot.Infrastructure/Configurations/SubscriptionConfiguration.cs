using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.ValueObjects;

namespace TelegramPhotoBot.Infrastructure.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");

        builder.HasKey(s => s.Id);

        // Configure DateRange as Value Object (Owned Entity)
        builder.OwnsOne(s => s.Period, period =>
        {
            period.Property(dr => dr.StartDate)
                .HasColumnName("StartDate")
                .IsRequired();

            period.Property(dr => dr.EndDate)
                .HasColumnName("EndDate")
                .IsRequired();
        });

        // Configure TelegramStars as Value Object (Owned Entity)
        builder.OwnsOne(s => s.PaidAmount, paidAmount =>
        {
            paidAmount.Property(s => s.Amount)
                .HasColumnName("PaidAmount")
                .IsRequired();
        });

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Relationships
        builder.HasOne(s => s.User)
            .WithMany(u => u.Subscriptions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.SubscriptionPlan)
            .WithMany(sp => sp.Subscriptions)
            .HasForeignKey(s => s.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(s => s.UserId)
            .HasDatabaseName("IX_Subscriptions_UserId");

        builder.HasIndex(s => s.Status)
            .HasDatabaseName("IX_Subscriptions_Status");
    }
}

