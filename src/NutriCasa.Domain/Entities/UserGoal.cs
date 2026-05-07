using NutriCasa.Domain.Common;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Domain.Entities;

public class UserGoal : AuditableEntity
{
    public Guid UserId { get; set; }
    public GoalType GoalType { get; set; }
    public decimal StartWeightKg { get; set; }
    public decimal? TargetWeightKg { get; set; }
    public DateOnly? TargetDate { get; set; }
    public string? MotivationText { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? AchievedAt { get; set; }
    public DateTime? AbandonedAt { get; set; }

    public User User { get; set; } = null!;
}
