using NutriCasa.Domain.Common;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Domain.Entities;

public class PrivacySettings : BaseEntity
{
    public Guid UserId { get; set; }
    public VisibilityLevel ShareWeight { get; set; } = VisibilityLevel.Private;
    public VisibilityLevel ShareBodyFat { get; set; } = VisibilityLevel.Private;
    public VisibilityLevel ShareMeasurements { get; set; } = VisibilityLevel.Private;
    public VisibilityLevel SharePhotos { get; set; } = VisibilityLevel.Private;
    public VisibilityLevel ShareCheckIns { get; set; } = VisibilityLevel.Group;
    public bool AllowAiMentions { get; set; } = true;
    public bool AllowPush { get; set; } = true;
    public bool AllowEmail { get; set; } = true;
    public bool WeeklyDigest { get; set; } = true;
    public TimeOnly QuietHoursStart { get; set; } = new(21, 0);
    public TimeOnly QuietHoursEnd { get; set; } = new(8, 0);
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
