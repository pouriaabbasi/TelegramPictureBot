using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;
using TelegramPhotoBot.Domain.ValueObjects;

namespace TelegramPhotoBot.Infrastructure.Configurations;

public class PhotoConfiguration : IEntityTypeConfiguration<Photo>
{
    public void Configure(EntityTypeBuilder<Photo> builder)
    {
        builder.ToTable("Photos");

        builder.HasKey(p => p.Id);

        // Configure FileInfo as Value Object (Owned Entity)
        builder.OwnsOne(p => p.FileInfo, fileInfo =>
        {
            fileInfo.Property(fi => fi.FileId)
                .HasColumnName("FileId")
                .IsRequired()
                .HasMaxLength(500);

            fileInfo.Property(fi => fi.FileUniqueId)
                .HasColumnName("FileUniqueId")
                .HasMaxLength(500);

            fileInfo.Property(fi => fi.FilePath)
                .HasColumnName("FilePath")
                .HasMaxLength(1000);

            fileInfo.Property(fi => fi.MimeType)
                .HasColumnName("MimeType")
                .HasMaxLength(100);

            fileInfo.Property(fi => fi.FileSize)
                .HasColumnName("FileSize");

            fileInfo.Property(fi => fi.Width)
                .HasColumnName("Width");

            fileInfo.Property(fi => fi.Height)
                .HasColumnName("Height");
        });

        // Configure TelegramStars as Value Object (Owned Entity)
        builder.OwnsOne(p => p.Price, price =>
        {
            price.Property(s => s.Amount)
                .HasColumnName("Price")
                .IsRequired();
        });

        builder.Property(p => p.Caption)
            .HasMaxLength(1000);
        
        // Marketplace properties
        builder.Property(p => p.ModelId)
            .IsRequired();
        
        builder.Property(p => p.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(p => p.ViewCount)
            .IsRequired()
            .HasDefaultValue(0);

        // Relationships
        builder.HasOne(p => p.Seller)
            .WithMany(u => u.Photos)
            .HasForeignKey(p => p.SellerId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(p => p.Model)
            .WithMany(m => m.Photos)
            .HasForeignKey(p => p.ModelId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(p => p.SellerId)
            .HasDatabaseName("IX_Photos_SellerId");

        builder.HasIndex(p => p.IsForSale)
            .HasDatabaseName("IX_Photos_IsForSale");
        
        builder.HasIndex(p => p.ModelId)
            .HasDatabaseName("IX_Photos_ModelId")
            .HasFilter("[IsDeleted] = 0");
        
        builder.HasIndex(p => new { p.ModelId, p.Type })
            .HasDatabaseName("IX_Photos_ModelId_Type")
            .HasFilter("[IsDeleted] = 0");
    }
}

