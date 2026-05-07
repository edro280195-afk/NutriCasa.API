using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("token_id").HasDefaultValueSql("uuid_generate_v4()");
        builder.Property(e => e.TokenHash).HasMaxLength(255).IsRequired();
        builder.Property(e => e.IpAddress).HasColumnType("inet");
        builder.HasOne(e => e.User).WithMany(u => u.RefreshTokens).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.UserId);
    }
}
