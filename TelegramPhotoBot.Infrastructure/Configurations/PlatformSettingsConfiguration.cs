using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Infrastructure.Configurations;

public class PlatformSettingsConfiguration : IEntityTypeConfiguration<PlatformSettings>
{
    public void Configure(EntityTypeBuilder<PlatformSettings> builder)
    {
        builder.ToTable("PlatformSettings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Key)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Value)
            .IsRequired()
            .HasMaxLength(4000); // Large enough for session data

        builder.Property(s => s.Description)
            .HasMaxLength(500);

        builder.Property(s => s.IsEncrypted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.IsSecret)
            .IsRequired()
            .HasDefaultValue(false);

        // Unique index on Key (filtered to exclude soft-deleted)
        builder.HasIndex(s => s.Key)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Index for filtering non-secret settings
        builder.HasIndex(s => new { s.IsSecret, s.IsDeleted });
    }
}

