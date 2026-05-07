using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Infrastructure.Persistence.Configurations;

public class UserTrialUsedConfiguration : IEntityTypeConfiguration<UserTrialUsed>
{
    public void Configure(EntityTypeBuilder<UserTrialUsed> builder)
    {
        builder.ToTable("user_trials_used");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("trial_id").HasDefaultValueSql("uuid_generate_v4()");
        builder.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Plan).WithMany().HasForeignKey(e => e.PlanId);
        builder.HasIndex(e => new { e.UserId, e.PlanId }).IsUnique();
    }
}
