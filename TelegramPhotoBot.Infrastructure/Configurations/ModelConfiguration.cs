using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;

namespace TelegramPhotoBot.Infrastructure.Configurations;

public class ModelConfiguration : IEntityTypeConfiguration<Model>
{
    public void Configure(EntityTypeBuilder<Model> builder)
    {
        builder.ToTable("Models");

        builder.HasKey(m => m.Id);

        // Properties
        builder.Property(m => m.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Bio)
            .HasMaxLength(500);

        builder.Property(m => m.Status)
            .IsRequired()
            .HasConversion<int>(); // Store enum as int

        builder.Property(m => m.ApprovedAt)
            .IsRequired(false);

        builder.Property(m => m.ApprovedByAdminId)
            .IsRequired(false);

        builder.Property(m => m.RejectionReason)
            .HasMaxLength(500);

        builder.Property(m => m.TotalSubscribers)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(m => m.TotalContentItems)
            .IsRequired()
            .HasDefaultValue(0);

        // Owned entity: DemoImage
        builder.OwnsOne(m => m.DemoImage, demoImage =>
        {
            demoImage.Property(f => f.FileId)
                .HasColumnName("DemoImage_FileId")
                .HasMaxLength(100);

            demoImage.Property(f => f.FilePath)
                .HasColumnName("DemoImage_FilePath")
                .HasMaxLength(500);

            demoImage.Property(f => f.FileSize)
                .HasColumnName("DemoImage_FileSize");

            demoImage.Property(f => f.Width)
                .HasColumnName("DemoImage_Width");

            demoImage.Property(f => f.Height)
                .HasColumnName("DemoImage_Height");

            demoImage.Property(f => f.MimeType)
                .HasColumnName("DemoImage_MimeType")
                .HasMaxLength(50);

            demoImage.Property(f => f.FileUniqueId)
                .HasColumnName("DemoImage_FileUniqueId")
                .HasMaxLength(100);
        });

        // Owned entity: SubscriptionPrice (optional)
        builder.OwnsOne(m => m.SubscriptionPrice, price =>
        {
            price.Property(p => p.Amount)
                .HasColumnName("SubscriptionPrice");
        });

        builder.Property(m => m.SubscriptionDurationDays)
            .IsRequired(false);

        // Relationships
        builder.HasOne(m => m.User)
            .WithOne(u => u.Model)
            .HasForeignKey<Model>(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(m => m.Photos)
            .WithOne(p => p.Model)
            .HasForeignKey(p => p.ModelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(m => m.Subscriptions)
            .WithOne(s => s.Model)
            .HasForeignKey(s => s.ModelId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(m => m.UserId)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0"); // Allow same UserId after soft delete

        builder.HasIndex(m => m.Status)
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(m => m.DisplayName)
            .HasFilter("[IsDeleted] = 0 AND [Status] = 1"); // Approved models only

        // Soft delete query filter
        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}

