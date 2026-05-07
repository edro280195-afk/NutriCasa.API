using NutriCasa.Domain.Common;

namespace NutriCasa.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public string EntityType { get; set; } = null!;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = null!;
    public string? OldValues { get; set; } // JSONB
    public string? NewValues { get; set; } // JSONB
    public System.Net.IPAddress? IpAddress { get; set; } // INET
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
}
