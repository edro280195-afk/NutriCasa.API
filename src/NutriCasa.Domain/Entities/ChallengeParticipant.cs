using NutriCasa.Domain.Common;

namespace NutriCasa.Domain.Entities;

public class ChallengeParticipant : BaseEntity
{
    public Guid ChallengeId { get; set; }
    public Guid UserId { get; set; }
    public Guid? SubGroupId { get; set; }
    public decimal? StartingValue { get; set; }
    public decimal? CurrentValue { get; set; }
    public decimal? FinalScore { get; set; }
    public int? FinalPosition { get; set; }
    public DateTime JoinedAt { get; set; }

    public Challenge Challenge { get; set; } = null!;
    public User User { get; set; } = null!;
    public Group? SubGroup { get; set; }
}
