using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Infrastructure.Persistence.Configurations;

public class KetoProfileConfiguration : IEntityTypeConfiguration<KetoProfile>
{
    public void Configure(EntityTypeBuilder<KetoProfile> builder)
    {
        builder.ToTable("keto_profiles");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("profile_id").HasDefaultValueSql("uuid_generate_v4()");
        builder.Property(e => e.CarbsGrams).HasColumnType("decimal(6,2)");
        builder.Property(e => e.ProteinGrams).HasColumnType("decimal(6,2)");
        builder.Property(e => e.FatGrams).HasColumnType("decimal(6,2)");
        builder.Property(e => e.CarbsPercent).HasColumnType("decimal(5,2)");
        builder.Property(e => e.ProteinPercent).HasColumnType("decimal(5,2)");
        builder.Property(e => e.FatPercent).HasColumnType("decimal(5,2)");
        builder.Property(e => e.CalculationMethod).HasMaxLength(30);
        builder.Property(e => e.TargetWeeklyCostMxn).HasColumnType("decimal(10,2)");
        builder.Property(e => e.TargetMealCostMxn).HasColumnType("decimal(10,2)");
        builder.HasOne(e => e.User).WithOne(u => u.KetoProfile).HasForeignKey<KetoProfile>(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
