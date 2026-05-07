using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("user_id").HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(e => e.FullName).HasMaxLength(120).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(255).IsRequired();
        builder.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
        builder.Property(e => e.HeightCm).HasColumnType("decimal(5,2)");
        builder.Property(e => e.Gender).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.ActivityLevel).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.BodyTypeSelected).HasMaxLength(20);
        builder.Property(e => e.Timezone).HasMaxLength(50).HasDefaultValue("America/Mexico_City");
        builder.Property(e => e.PreferredLanguage).HasMaxLength(10).HasDefaultValue("es-MX");
        builder.Property(e => e.NutritionTrack).HasConversion<string>().HasMaxLength(20).HasDefaultValue(NutritionTrack.Keto);

        builder.HasIndex(e => e.Email).IsUnique();

        builder.HasOne(e => e.TutorUser).WithMany().HasForeignKey(e => e.TutorUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.DisclaimerVersion).WithMany().HasForeignKey(e => e.DisclaimerVersionId);
        builder.HasOne(e => e.TutorConsentVersion).WithMany().HasForeignKey(e => e.TutorConsentVersionId);
        builder.HasOne(e => e.BudgetMode).WithMany().HasForeignKey(e => e.BudgetModeId).OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(e => e.DeletedAt == null);
    }
}
