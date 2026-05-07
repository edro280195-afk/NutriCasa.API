using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Infrastructure.Persistence.Configurations;

public class DailyCheckInConfiguration : IEntityTypeConfiguration<DailyCheckIn>
{
    public void Configure(EntityTypeBuilder<DailyCheckIn> builder)
    {
        builder.ToTable("daily_check_ins");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("check_in_id").HasDefaultValueSql("uuid_generate_v4()");
        builder.Property(e => e.SleepHours).HasColumnType("decimal(3,1)");
        builder.Property(e => e.WaterLiters).HasColumnType("decimal(3,1)");
        builder.Property(e => e.KetonesMmol).HasColumnType("decimal(4,2)");
        builder.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.UserId, e.CheckInDate }).IsUnique();
    }
}
