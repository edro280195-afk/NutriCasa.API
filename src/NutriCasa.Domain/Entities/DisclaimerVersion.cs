using NutriCasa.Domain.Common;

namespace NutriCasa.Domain.Entities;

public class DisclaimerVersion : BaseEntity
{
    public string DisclaimerType { get; set; } = null!; // 'general','override','tutor'
    public string VersionCode { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateOnly EffectiveFrom { get; set; }
    public bool IsCurrent { get; set; }
    public DateTime CreatedAt { get; set; }
}
