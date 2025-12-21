using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.ValueObjects;

namespace TelegramPhotoBot.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        // Configure TelegramUserId as Value Object (Owned Entity)
        builder.OwnsOne(u => u.TelegramUserId, telegramUserId =>
        {
            telegramUserId.Property(tu => tu.Value)
                .HasColumnName("TelegramUserId")
                .IsRequired();
        });

        builder.Property(u => u.Username)
            .HasMaxLength(100);

        builder.Property(u => u.FirstName)
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .HasMaxLength(100);

        builder.Property(u => u.LanguageCode)
            .HasMaxLength(10);

        // Configure backing fields for collections
        builder.Metadata.FindNavigation(nameof(User.UserRoles))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Metadata.FindNavigation(nameof(User.Photos))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Metadata.FindNavigation(nameof(User.Subscriptions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Metadata.FindNavigation(nameof(User.Purchases))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // Indexes
        builder.HasIndex(u => u.TelegramUserId.Value)
            .IsUnique()
            .HasDatabaseName("IX_Users_TelegramUserId");

        builder.HasIndex(u => u.Username)
            .HasDatabaseName("IX_Users_Username");
    }
}

