using NutriCasa.Domain.Common;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public string Type { get; set; } = null!;
    public NotificationPriority Priority { get; set; } = NotificationPriority.P3;
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string? DeepLink { get; set; }
    public string? IconUrl { get; set; }
    public string? Metadata { get; set; } // JSONB
    public string[] DeliveryChannels { get; set; } = ["in_app"];
    public DateTime? SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
