using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.ValueObjects;

namespace TelegramPhotoBot.Infrastructure.Configurations;

public class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        // Table Per Hierarchy (TPH) - All purchase types in a single table with discriminator
        builder.ToTable("Purchases");
        
        // Configure TPH discriminator
        builder.HasDiscriminator<string>("PurchaseType")
            .HasValue<Purchase>("Purchase")
            .HasValue<PurchasePhoto>("PurchasePhoto")
            .HasValue<ModelSubscription>("ModelSubscription");

        builder.HasKey(p => p.Id);

        // Configure TelegramStars as Value Object (Owned Entity)
        builder.OwnsOne(p => p.Amount, amount =>
        {
            amount.Property(s => s.Amount)
                .HasColumnName("Amount")
                .IsRequired();
        });

        builder.Property(p => p.PurchaseDate)
            .IsRequired();

        builder.Property(p => p.PaymentStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(p => p.TelegramPaymentId)
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(p => p.TelegramPreCheckoutQueryId)
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(p => p.PaymentVerifiedAt)
            .IsRequired(false);

        // Index for payment verification (prevent duplicate processing)
        builder.HasIndex(p => p.TelegramPaymentId)
            .HasDatabaseName("IX_Purchases_TelegramPaymentId")
            .IsUnique()
            .HasFilter("[TelegramPaymentId] IS NOT NULL");

        // Relationships
        builder.HasOne(p => p.User)
            .WithMany(u => u.Purchases)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(p => p.UserId)
            .HasDatabaseName("IX_Purchases_UserId");

        builder.HasIndex(p => p.PurchaseDate)
            .HasDatabaseName("IX_Purchases_PurchaseDate");
    }
}

