using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Infrastructure.Configurations;

public class ViewHistoryConfiguration : IEntityTypeConfiguration<ViewHistory>
{
    public void Configure(EntityTypeBuilder<ViewHistory> builder)
    {
        builder.ToTable("ViewHistories");

        builder.HasKey(vh => vh.Id);

        builder.Property(vh => vh.UserId)
            .IsRequired();

        builder.Property(vh => vh.PhotoId)
            .IsRequired();

        builder.Property(vh => vh.ModelId)
            .IsRequired();

        builder.Property(vh => vh.PhotoType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(vh => vh.ViewedAt)
            .IsRequired();

        builder.Property(vh => vh.ViewerUsername)
            .HasMaxLength(100);

        builder.Property(vh => vh.PhotoCaption)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(vh => vh.User)
            .WithMany()
            .HasForeignKey(vh => vh.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(vh => vh.Photo)
            .WithMany()
            .HasForeignKey(vh => vh.PhotoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(vh => vh.Model)
            .WithMany()
            .HasForeignKey(vh => vh.ModelId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for performance
        builder.HasIndex(vh => vh.UserId);
        builder.HasIndex(vh => vh.PhotoId);
        builder.HasIndex(vh => vh.ModelId);
        builder.HasIndex(vh => vh.ViewedAt);
        builder.HasIndex(vh => new { vh.UserId, vh.PhotoId });
        builder.HasIndex(vh => new { vh.ModelId, vh.ViewedAt });
    }
}

