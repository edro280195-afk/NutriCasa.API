using NutriCasa.Domain.Common;

namespace NutriCasa.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? UserAgent { get; set; }
    public System.Net.IPAddress? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
