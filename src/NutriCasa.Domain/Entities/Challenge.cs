using NutriCasa.Domain.Common;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Domain.Entities;

public class Challenge : AuditableEntity
{
    public Guid GroupId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public ChallengeGoalType GoalType { get; set; }
    public string? GoalDescription { get; set; }
    public string? RewardDescription { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFinalized { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }

    public Group Group { get; set; } = null!;
    public User? CreatedByUser { get; set; }
    public ICollection<ChallengeParticipant> Participants { get; set; } = new List<ChallengeParticipant>();
}
