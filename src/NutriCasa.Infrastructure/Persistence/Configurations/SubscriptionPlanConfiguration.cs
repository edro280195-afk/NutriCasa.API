using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Infrastructure.Persistence.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("subscription_plans");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("plan_id").HasDefaultValueSql("uuid_generate_v4()");
        builder.Property(e => e.Code).HasMaxLength(30).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(50).IsRequired();
        builder.Property(e => e.PriceMonthlyMxn).HasColumnType("decimal(10,2)");
        builder.Property(e => e.PriceYearlyMxn).HasColumnType("decimal(10,2)");
        builder.Property(e => e.Features).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        builder.HasIndex(e => e.Code).IsUnique();
    }
}
