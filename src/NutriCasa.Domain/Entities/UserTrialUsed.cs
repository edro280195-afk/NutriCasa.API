using NutriCasa.Domain.Common;

namespace NutriCasa.Domain.Entities;

public class UserTrialUsed : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }
    public DateTime TrialStartedAt { get; set; }
    public DateTime? TrialEndedAt { get; set; }
    public bool ConvertedToPaid { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public SubscriptionPlan Plan { get; set; } = null!;
}
