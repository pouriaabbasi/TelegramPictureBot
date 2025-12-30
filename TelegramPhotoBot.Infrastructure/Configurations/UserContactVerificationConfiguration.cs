using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Infrastructure.Configurations;

public class UserContactVerificationConfiguration : IEntityTypeConfiguration<UserContactVerification>
{
    public void Configure(EntityTypeBuilder<UserContactVerification> builder)
    {
        builder.ToTable("UserContactVerifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.IsAutoAddedToSenderContacts)
            .IsRequired();

        builder.Property(x => x.IsMutualContact)
            .IsRequired();

        builder.Property(x => x.IsAdminNotified)
            .IsRequired();

        builder.Property(x => x.IsUserInstructedToAddContact)
            .IsRequired();

        builder.Property(x => x.HasUserSentMessage)
            .IsRequired();

        builder.Property(x => x.LastCheckedAt)
            .IsRequired();

        builder.Property(x => x.LastErrorMessage)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.HasIndex(x => x.IsMutualContact);
    }
}

