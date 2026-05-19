using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Challenges.Services;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Application.Features.Challenges.Queries.GetChallengeLeaderboard;

public record GetChallengeLeaderboardQuery : IRequest<Result<ChallengeLeaderboardDto>>
{
    public required Guid ChallengeId { get; init; }
}

public record ChallengeLeaderboardDto
{
    public Guid ChallengeId { get; set; }
    public string Title { get; set; } = "";
    public string GoalType { get; set; } = "";
    public List<ChallengeLeaderboardEntry> Entries { get; set; } = [];
}

public record ChallengeLeaderboardEntry
{
    public int Rank { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = "";
    public string CurrentValueDisplay { get; set; } = "";
    public decimal Score { get; set; }
    public bool IsCurrentUser { get; set; }
}

public class GetChallengeLeaderboardQueryHandler : IRequestHandler<GetChallengeLeaderboardQuery, Result<ChallengeLeaderboardDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetChallengeLeaderboardQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ChallengeLeaderboardDto>> Handle(GetChallengeLeaderboardQuery request, CancellationToken ct)
    {
        if (_currentUserService.UserId is null)
            return Result<ChallengeLeaderboardDto>.Failure("No autenticado.", "UNAUTHORIZED");

        var challenge = await _context.Challenges
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(c => c.Id == request.ChallengeId, ct);

        if (challenge is null)
            return Result<ChallengeLeaderboardDto>.Failure("Reto no encontrado.", "NOT_FOUND");

        var userId = _currentUserService.UserId.Value;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var entries = new List<(ChallengeParticipant p, decimal score, string display)>();

        foreach (var p in challenge.Participants)
        {
            var (currentValue, streak, checkIns, adherence) = await GetCurrentStatsAsync(p.UserId, challenge.GoalType, challenge.StartDate, today, ct);

            var score = ChallengeScoringService.CalculateScore(challenge.GoalType, p.StartingValue, currentValue, streak, checkIns, adherence);
            var display = ChallengeScoringService.FormatValue(challenge.GoalType, p.StartingValue, currentValue, streak, checkIns, adherence);

            entries.Add((p, score, display));
        }

        var ranked = entries
            .OrderByDescending(e => e.score)
            .Select((e, i) => new ChallengeLeaderboardEntry
            {
                Rank = i + 1,
                UserId = e.p.UserId,
                FullName = e.p.User?.FullName ?? "Alguien",
                CurrentValueDisplay = e.display,
                Score = e.score,
                IsCurrentUser = e.p.UserId == userId,
            })
            .ToList();

        return Result<ChallengeLeaderboardDto>.Success(new ChallengeLeaderboardDto
        {
            ChallengeId = challenge.Id,
            Title = challenge.Title,
            GoalType = challenge.GoalType.ToString(),
            Entries = ranked,
        });
    }

    private async Task<(decimal? currentValue, int streak, int checkIns, decimal adherence)> GetCurrentStatsAsync(
        Guid userId, Domain.Enums.ChallengeGoalType goalType, DateOnly startDate, DateOnly today, CancellationToken ct)
    {
        decimal? currentValue = null;
        int streak = 0;
        int checkIns = 0;
        decimal adherence = 0;

        if (goalType is Domain.Enums.ChallengeGoalType.MostWeightLoss or Domain.Enums.ChallengeGoalType.MostFatLoss)
        {
            currentValue = await _context.BodyMeasurements
                .Where(m => m.UserId == userId && m.MeasuredAt >= startDate)
                .OrderByDescending(m => m.MeasuredAt)
                .Select(m => (decimal?)m.WeightKg)
                .FirstOrDefaultAsync(ct);
        }

        if (goalType is Domain.Enums.ChallengeGoalType.BestStreak)
        {
            var checkInDates = await _context.DailyCheckIns
                .Where(c => c.UserId == userId && c.CheckInDate >= startDate)
                .OrderByDescending(c => c.CheckInDate)
                .Select(c => c.CheckInDate)
                .ToListAsync(ct);

            streak = CalculateLongestStreak(checkInDates);
        }

        if (goalType is Domain.Enums.ChallengeGoalType.MostCheckIns)
        {
            checkIns = await _context.DailyCheckIns
                .CountAsync(c => c.UserId == userId && c.CheckInDate >= startDate, ct);
        }

        if (goalType is Domain.Enums.ChallengeGoalType.HighestAdherence)
        {
            var totalDays = Math.Max(1, (today.DayNumber - startDate.DayNumber));
            var checkInCount = await _context.DailyCheckIns
                .CountAsync(c => c.UserId == userId && c.CheckInDate >= startDate, ct);
            adherence = Math.Round((decimal)checkInCount / totalDays * 100, 1);
        }

        return (currentValue, streak, checkIns, adherence);
    }

    private static int CalculateLongestStreak(List<DateOnly> dates)
    {
        if (dates.Count == 0) return 0;

        dates = dates.OrderBy(d => d).ToList();
        int maxStreak = 1;
        int currentStreak = 1;

        for (int i = 1; i < dates.Count; i++)
        {
            if ((dates[i].DayNumber - dates[i - 1].DayNumber) == 1)
            {
                currentStreak++;
                maxStreak = Math.Max(maxStreak, currentStreak);
            }
            else
            {
                currentStreak = 1;
            }
        }

        return maxStreak;
    }
}
