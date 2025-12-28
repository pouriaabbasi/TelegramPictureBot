using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Infrastructure.Configurations;

public class MtProtoAccessTokenConfiguration : IEntityTypeConfiguration<MtProtoAccessToken>
{
    public void Configure(EntityTypeBuilder<MtProtoAccessToken> builder)
    {
        builder.ToTable("MtProtoAccessTokens");
        
        builder.HasKey(e => e.Token);
        
        builder.Property(e => e.Token)
            .IsRequired()
            .ValueGeneratedNever();
        
        builder.Property(e => e.AdminUserId)
            .IsRequired();
        
        builder.Property(e => e.CreatedAt)
            .IsRequired();
        
        builder.Property(e => e.ExpiresAt)
            .IsRequired();
        
        builder.Property(e => e.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);
        
        builder.Property(e => e.UsedAt);
        
        // Index for cleanup queries
        builder.HasIndex(e => e.ExpiresAt);
        
        // Index for admin user
        builder.HasIndex(e => e.AdminUserId);
    }
}

