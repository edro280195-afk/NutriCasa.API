using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Infrastructure.Persistence.Configurations;

public class MedicalProfileConfiguration : IEntityTypeConfiguration<MedicalProfile>
{
    public void Configure(EntityTypeBuilder<MedicalProfile> builder)
    {
        builder.ToTable("medical_profiles");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("profile_id").HasDefaultValueSql("uuid_generate_v4()");
        builder.Property(e => e.DiabetesType).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.KetoExperienceLevel).HasConversion<string>().HasMaxLength(20);
        builder.HasOne(e => e.User).WithOne(u => u.MedicalProfile).HasForeignKey<MedicalProfile>(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.OverrideDisclaimerVersion).WithMany().HasForeignKey(e => e.OverrideDisclaimerVersionId);
    }
}
