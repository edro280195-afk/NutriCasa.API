using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Infrastructure.Persistence.Configurations;

public class UserSubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
{
    public void Configure(EntityTypeBuilder<UserSubscription> builder)
    {
        builder.ToTable("user_subscriptions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("subscription_id").HasDefaultValueSql("uuid_generate_v4()");
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Metadata).HasColumnType("jsonb");
        builder.HasOne(e => e.User).WithMany(u => u.UserSubscriptions).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Plan).WithMany().HasForeignKey(e => e.PlanId);
        builder.HasIndex(e => new { e.UserId, e.Status });
    }
}
