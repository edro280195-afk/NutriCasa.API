using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Infrastructure.Persistence.Configurations;

public class PrivacySettingsConfiguration : IEntityTypeConfiguration<PrivacySettings>
{
    public void Configure(EntityTypeBuilder<PrivacySettings> builder)
    {
        builder.ToTable("privacy_settings");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("settings_id").HasDefaultValueSql("uuid_generate_v4()");
        builder.Property(e => e.ShareWeight).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.ShareBodyFat).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.ShareMeasurements).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.SharePhotos).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.ShareCheckIns).HasConversion<string>().HasMaxLength(20);
        builder.HasOne(e => e.User).WithOne(u => u.PrivacySettings).HasForeignKey<PrivacySettings>(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
