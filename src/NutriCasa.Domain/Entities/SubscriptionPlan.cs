using NutriCasa.Domain.Common;

namespace NutriCasa.Domain.Entities;

public class SubscriptionPlan : AuditableEntity
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal PriceMonthlyMxn { get; set; }
    public decimal? PriceYearlyMxn { get; set; }
    public int TrialDays { get; set; }
    public int? MaxGroupMembers { get; set; }
    public int? MaxRegenerationsWeek { get; set; }
    public int? MaxSwapsWeek { get; set; }
    public int? MaxChatMessagesMonth { get; set; }
    public bool HasAiChat { get; set; }
    public bool HasPhotoAnalysis { get; set; }
    public bool HasAdvancedAnalytics { get; set; }
    public bool HasPrioritySupport { get; set; }
    public string Features { get; set; } = "{}"; // JSONB
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
