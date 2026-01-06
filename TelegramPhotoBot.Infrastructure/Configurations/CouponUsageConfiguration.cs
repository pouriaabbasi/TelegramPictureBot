using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Infrastructure.Configurations;

public class CouponUsageConfiguration : IEntityTypeConfiguration<CouponUsage>
{
    public void Configure(EntityTypeBuilder<CouponUsage> builder)
    {
        builder.ToTable("CouponUsages");

        builder.HasKey(cu => cu.Id);

        builder.Property(cu => cu.CouponId)
            .IsRequired();

        builder.Property(cu => cu.UserId)
            .IsRequired();

        builder.Property(cu => cu.UsedAt)
            .IsRequired();

        builder.Property(cu => cu.OriginalPriceStars)
            .IsRequired();

        builder.Property(cu => cu.DiscountAmountStars)
            .IsRequired();

        builder.Property(cu => cu.FinalPriceStars)
            .IsRequired();

        builder.Property(cu => cu.ModelShareStars)
            .IsRequired();

        builder.Property(cu => cu.PlatformShareStars)
            .IsRequired();

        builder.Property(cu => cu.PhotoId)
            .IsRequired(false);

        builder.Property(cu => cu.ModelId)
            .IsRequired(false);

        // Indexes for efficient queries
        builder.HasIndex(cu => new { cu.CouponId, cu.UserId });
        builder.HasIndex(cu => cu.UsedAt);

        // Relationships
        builder.HasOne(cu => cu.Coupon)
            .WithMany(c => c.Usages)
            .HasForeignKey(cu => cu.CouponId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cu => cu.User)
            .WithMany()
            .HasForeignKey(cu => cu.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cu => cu.Photo)
            .WithMany()
            .HasForeignKey(cu => cu.PhotoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cu => cu.Model)
            .WithMany()
            .HasForeignKey(cu => cu.ModelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
