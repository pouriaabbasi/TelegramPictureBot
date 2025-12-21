using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.ValueObjects;

namespace TelegramPhotoBot.Infrastructure.Configurations;

public class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        // Table Per Type (TPT) - هر کلاس جدول جداگانه دارد
        builder.ToTable("Purchases");

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

