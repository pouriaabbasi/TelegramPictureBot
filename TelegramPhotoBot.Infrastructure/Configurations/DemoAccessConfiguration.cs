using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Infrastructure.Configurations;

public class DemoAccessConfiguration : IEntityTypeConfiguration<DemoAccess>
{
    public void Configure(EntityTypeBuilder<DemoAccess> builder)
    {
        builder.ToTable("DemoAccesses");

        builder.HasKey(da => da.Id);

        builder.Property(da => da.UserId)
            .IsRequired();

        builder.Property(da => da.ModelId)
            .IsRequired();

        builder.Property(da => da.AccessedAt)
            .IsRequired();

        builder.Property(da => da.DemoFileId)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(da => da.User)
            .WithMany()
            .HasForeignKey(da => da.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(da => da.Model)
            .WithMany()
            .HasForeignKey(da => da.ModelId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(da => new { da.UserId, da.ModelId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0"); // Only one access per user per model (excluding soft-deleted)

        builder.HasIndex(da => da.AccessedAt);
    }
}

