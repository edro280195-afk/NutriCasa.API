using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Infrastructure.Persistence.Configurations;

public class UserGoalConfiguration : IEntityTypeConfiguration<UserGoal>
{
    public void Configure(EntityTypeBuilder<UserGoal> builder)
    {
        builder.ToTable("user_goals");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("goal_id").HasDefaultValueSql("uuid_generate_v4()");
        builder.Property(e => e.GoalType).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.StartWeightKg).HasColumnType("decimal(5,2)");
        builder.Property(e => e.TargetWeightKg).HasColumnType("decimal(5,2)");
        builder.HasOne(e => e.User).WithMany(u => u.UserGoals).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.UserId, e.IsActive });
    }
}
