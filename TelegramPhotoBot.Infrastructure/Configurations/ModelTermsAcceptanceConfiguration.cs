using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TelegramPhotoBot.Domain.Entities;

namespace TelegramPhotoBot.Infrastructure.Configurations;

public class ModelTermsAcceptanceConfiguration : IEntityTypeConfiguration<ModelTermsAcceptance>
{
    public void Configure(EntityTypeBuilder<ModelTermsAcceptance> builder)
    {
        builder.ToTable("ModelTermsAcceptances");

        builder.HasKey(mta => mta.Id);

        builder.Property(mta => mta.ModelId)
            .IsRequired();

        builder.Property(mta => mta.AcceptedAt)
            .IsRequired();

        builder.Property(mta => mta.TermsVersion)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(mta => mta.TermsContent)
            .IsRequired()
            .HasMaxLength(10000); // Store full terms content

        builder.Property(mta => mta.IsLatestVersion)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(mta => mta.Notes)
            .HasMaxLength(1000);

        // Relationship with Model
        builder.HasOne(mta => mta.Model)
            .WithMany()
            .HasForeignKey(mta => mta.ModelId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(mta => mta.ModelId);
        builder.HasIndex(mta => new { mta.ModelId, mta.IsLatestVersion });
        builder.HasIndex(mta => mta.AcceptedAt);
        builder.HasIndex(mta => mta.TermsVersion);

        // Soft delete filter
        builder.HasQueryFilter(mta => !mta.IsDeleted);
    }
}
