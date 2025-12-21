using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Infrastructure.Configurations;

public class PurchasePhotoConfiguration : IEntityTypeConfiguration<PurchasePhoto>
{
    public void Configure(EntityTypeBuilder<PurchasePhoto> builder)
    {
        // Table Per Type (TPT) - جدول جداگانه برای PurchasePhoto
        builder.ToTable("PurchasePhotos");

        // Relationships specific to PurchasePhoto
        builder.HasOne(pp => pp.Photo)
            .WithMany(p => p.Purchases)
            .HasForeignKey(pp => pp.PhotoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(pp => pp.PhotoId)
            .HasDatabaseName("IX_PurchasePhoto_PhotoId");
    }
}

