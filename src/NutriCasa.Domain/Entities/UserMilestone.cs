using NutriCasa.Domain.Common;

namespace NutriCasa.Domain.Entities;

public class UserMilestone : BaseEntity
{
    public Guid UserId { get; set; }
    public string MilestoneType { get; set; } = null!;
    public decimal? MilestoneValue { get; set; }
    public DateTime AchievedAt { get; set; }
    public bool PostedToGroup { get; set; }

    public User User { get; set; } = null!;
}
