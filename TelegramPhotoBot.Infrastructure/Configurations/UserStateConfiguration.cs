using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Infrastructure.Configurations;

public class UserStateConfiguration : IEntityTypeConfiguration<UserState>
{
    public void Configure(EntityTypeBuilder<UserState> builder)
    {
        builder.ToTable("UserStates");

        builder.HasKey(us => us.Id);

        builder.Property(us => us.UserId)
            .IsRequired();

        builder.Property(us => us.StateType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(us => us.StateData)
            .HasMaxLength(2000); // JSON data

        builder.Property(us => us.ExpiresAt)
            .IsRequired();

        // Relationships
        builder.HasOne(us => us.User)
            .WithMany()
            .HasForeignKey(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(us => us.UserId)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0"); // Only one active state per user

        builder.HasIndex(us => us.ExpiresAt);
    }
}

