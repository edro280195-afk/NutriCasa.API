using NutriCasa.Domain.Common;

namespace NutriCasa.Domain.Entities;

public class FeatureFlag : AuditableEntity
{
    public string Code { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }
    public int RolloutPercent { get; set; }
    public Guid[]? TargetUserIds { get; set; }
    public string? Metadata { get; set; } // JSONB
}
