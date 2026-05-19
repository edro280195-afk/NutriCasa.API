using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Features.Challenges.Services;

namespace NutriCasa.Infrastructure.BackgroundJobs;

public class ChallengeFinalizationJob
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ChallengeFinalizationJob> _logger;

    public ChallengeFinalizationJob(IApplicationDbContext context, ILogger<ChallengeFinalizationJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var expired = await _context.Challenges
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .Where(c => c.IsActive && !c.IsFinalized && c.EndDate <= today)
            .ToListAsync(ct);

        _logger.LogInformation("Finalizando {Count} retos vencidos...", expired.Count);

        foreach (var challenge in expired)
        {
            try
            {
                await FinalizeChallengeAsync(challenge, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al finalizar reto {ChallengeId}", challenge.Id);
            }
        }

        await _context.SaveChangesAsync(ct);
    }

    private async Task FinalizeChallengeAsync(Domain.Entities.Challenge challenge, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = challenge.StartDate;

        foreach (var participant in challenge.Participants)
        {
            var (currentValue, streak, checkIns, adherence) = await GetCurrentStatsAsync(
                participant.UserId, challenge.GoalType, startDate, today, ct);

            var score = ChallengeScoringService.CalculateScore(
                challenge.GoalType, participant.StartingValue, currentValue, streak, checkIns, adherence);

            participant.CurrentValue = currentValue;
            participant.FinalScore = score;
        }

        challenge.IsFinalized = true;

        _logger.LogInformation("Reto '{Title}' ({Id}) finalizado con {Count} participantes.",
            challenge.Title, challenge.Id, challenge.Participants.Count);
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
                .Where(m => m.UserId == userId)
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
