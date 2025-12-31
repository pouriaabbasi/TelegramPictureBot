using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Infrastructure.Configurations;

public class ModelPayoutConfiguration : IEntityTypeConfiguration<ModelPayout>
{
    public void Configure(EntityTypeBuilder<ModelPayout> builder)
    {
        builder.ToTable("ModelPayouts");

        builder.HasKey(mp => mp.Id);

        builder.Property(mp => mp.ModelId)
            .IsRequired();

        builder.Property(mp => mp.AmountStars)
            .IsRequired();

        builder.Property(mp => mp.AmountFiat)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(mp => mp.Currency)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(mp => mp.ExchangeRate)
            .IsRequired()
            .HasPrecision(18, 6);

        builder.Property(mp => mp.Method)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(mp => mp.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(Domain.Enums.PayoutStatus.Pending);

        builder.Property(mp => mp.TrackingNumber)
            .HasMaxLength(100);

        builder.Property(mp => mp.AdminNotes)
            .HasMaxLength(2000);

        builder.Property(mp => mp.RequestedAt)
            .IsRequired();

        builder.Property(mp => mp.CompletedAt);

        builder.Property(mp => mp.ProcessedByAdminId);

        // Relationships
        builder.HasOne(mp => mp.Model)
            .WithMany()
            .HasForeignKey(mp => mp.ModelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(mp => mp.ProcessedByAdmin)
            .WithMany()
            .HasForeignKey(mp => mp.ProcessedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(mp => mp.ModelId);
        builder.HasIndex(mp => mp.Status);
        builder.HasIndex(mp => mp.RequestedAt);
        builder.HasIndex(mp => mp.CompletedAt);
        builder.HasIndex(mp => new { mp.ModelId, mp.Status });

        // Soft delete filter
        builder.HasQueryFilter(mp => !mp.IsDeleted);
    }
}
