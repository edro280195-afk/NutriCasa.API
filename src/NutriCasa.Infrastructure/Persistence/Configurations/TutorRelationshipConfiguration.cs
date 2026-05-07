using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Infrastructure.Persistence.Configurations;

public class TutorRelationshipConfiguration : IEntityTypeConfiguration<TutorRelationship>
{
    public void Configure(EntityTypeBuilder<TutorRelationship> builder)
    {
        builder.ToTable("tutor_relationships");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("relationship_id").HasDefaultValueSql("uuid_generate_v4()");
        builder.Property(e => e.RelationshipType).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.VerificationStatus).HasConversion<string>().HasMaxLength(20);
        builder.HasOne(e => e.MinorUser).WithMany().HasForeignKey(e => e.MinorUserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.TutorUser).WithMany().HasForeignKey(e => e.TutorUserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.VerifiedByUser).WithMany().HasForeignKey(e => e.VerifiedByUserId);
        builder.HasIndex(e => new { e.MinorUserId, e.TutorUserId }).IsUnique();
    }
}
