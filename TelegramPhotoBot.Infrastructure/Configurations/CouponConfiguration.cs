using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Infrastructure.Configurations;

public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("Coupons");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(c => c.Code)
            .IsUnique();

        builder.Property(c => c.DiscountPercentage)
            .IsRequired();

        builder.Property(c => c.UsageType)
            .IsRequired();

        builder.Property(c => c.OwnerType)
            .IsRequired();

        builder.Property(c => c.ModelId)
            .IsRequired(false);

        builder.Property(c => c.ValidFrom)
            .IsRequired(false);

        builder.Property(c => c.ValidTo)
            .IsRequired(false);

        builder.Property(c => c.MaxUses)
            .IsRequired(false);

        builder.Property(c => c.CurrentUses)
            .IsRequired();

        builder.Property(c => c.IsActive)
            .IsRequired();

        // Relationships
        builder.HasOne(c => c.Model)
            .WithMany()
            .HasForeignKey(c => c.ModelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Usages)
            .WithOne(u => u.Coupon)
            .HasForeignKey(u => u.CouponId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
