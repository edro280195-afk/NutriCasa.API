namespace NutriCasa.Application.Features.Challenges.Queries.GetActiveChallenges;

public record ChallengeDto
{
    public Guid ChallengeId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string GoalType { get; init; }
    public string? GoalDescription { get; init; }
    public string? RewardDescription { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public int ParticipantCount { get; init; }
    public bool HasJoined { get; init; }
    public required string CreatedBy { get; init; }
    public decimal MyCurrentScore { get; init; }
}
