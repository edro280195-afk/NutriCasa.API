using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Infrastructure.Persistence.Configurations;

public class DisclaimerVersionConfiguration : IEntityTypeConfiguration<DisclaimerVersion>
{
    public void Configure(EntityTypeBuilder<DisclaimerVersion> builder)
    {
        builder.ToTable("disclaimer_versions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("version_id").HasDefaultValueSql("uuid_generate_v4()");
        builder.Property(e => e.DisclaimerType).HasMaxLength(20).IsRequired();
        builder.Property(e => e.VersionCode).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Content).IsRequired();
        builder.HasIndex(e => new { e.DisclaimerType, e.VersionCode }).IsUnique();
        builder.HasIndex(e => new { e.DisclaimerType, e.IsCurrent });
    }
}
