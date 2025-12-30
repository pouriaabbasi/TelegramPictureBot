using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramPhotoBot.Domain.Entities;
using TelegramPhotoBot.Domain.Enums;
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
            
            // Create index on the owned entity property
            telegramUserId.HasIndex(tu => tu.Value)
                .IsUnique()
                .HasDatabaseName("IX_Users_TelegramUserId");
        });

        builder.Property(u => u.Username)
            .HasMaxLength(100);

        builder.Property(u => u.FirstName)
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .HasMaxLength(100);

        builder.Property(u => u.LanguageCode)
            .HasMaxLength(10);
        
        // Marketplace properties
        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(UserRole.User);
        
        builder.Property(u => u.ModelId)
            .IsRequired(false);

        // Configure backing fields for collections
        builder.Metadata.FindNavigation(nameof(User.Photos))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        
        var modelSubscriptionsNav = builder.Metadata.FindNavigation(nameof(User.ModelSubscriptions));
        if (modelSubscriptionsNav != null)
        {
            modelSubscriptionsNav.SetPropertyAccessMode(PropertyAccessMode.Field);
            // Explicitly configure that ModelSubscriptions uses the same foreign key as Purchases (UserId from Purchase base class)
            modelSubscriptionsNav.SetIsEagerLoaded(false);
        }

        var purchasesNav = builder.Metadata.FindNavigation(nameof(User.Purchases));
        if (purchasesNav != null)
        {
            purchasesNav.SetPropertyAccessMode(PropertyAccessMode.Field);
        }

        // Indexes
        builder.HasIndex(u => u.Username)
            .HasDatabaseName("IX_Users_Username");
        
        builder.HasIndex(u => u.Role)
            .HasDatabaseName("IX_Users_Role")
            .HasFilter("[IsDeleted] = 0");
    }
}

