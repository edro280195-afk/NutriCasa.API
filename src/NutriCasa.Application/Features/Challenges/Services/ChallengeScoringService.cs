using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.Challenges.Services;

public static class ChallengeScoringService
{
    public static decimal CalculateScore(
        ChallengeGoalType goalType,
        decimal? startingValue,
        decimal? currentValue,
        int longestStreak,
        int totalCheckIns,
        decimal adherencePercent)
    {
        return goalType switch
        {
            ChallengeGoalType.MostWeightLoss => CalculateWeightLossScore(startingValue, currentValue),
            ChallengeGoalType.MostFatLoss => CalculateWeightLossScore(startingValue, currentValue),
            ChallengeGoalType.BestStreak => CalculateStreakScore(longestStreak),
            ChallengeGoalType.HighestAdherence => CalculateAdherenceScore(adherencePercent),
            ChallengeGoalType.MostCheckIns => CalculateCheckInScore(totalCheckIns),
            ChallengeGoalType.Custom => 0,
            _ => 0,
        };
    }

    public static string FormatValue(
        ChallengeGoalType goalType,
        decimal? startingValue,
        decimal? currentValue,
        int longestStreak,
        int totalCheckIns,
        decimal adherencePercent)
    {
        return goalType switch
        {
            ChallengeGoalType.MostWeightLoss or ChallengeGoalType.MostFatLoss
                => FormatWeightLoss(startingValue, currentValue),
            ChallengeGoalType.BestStreak => $"{longestStreak} días",
            ChallengeGoalType.HighestAdherence => $"{adherencePercent:F1}%",
            ChallengeGoalType.MostCheckIns => $"{totalCheckIns} check-ins",
            ChallengeGoalType.Custom => "-",
            _ => "-",
        };
    }

    private static decimal CalculateWeightLossScore(decimal? start, decimal? current)
    {
        if (start is null or 0 || current is null)
            return 0;

        var loss = start.Value - current.Value;
        var percent = loss / start.Value * 100;
        return Math.Max(0, Math.Round(percent, 2));
    }

    private static decimal CalculateStreakScore(int streak)
    {
        return streak;
    }

    private static decimal CalculateAdherenceScore(decimal adherencePercent)
    {
        return Math.Round(adherencePercent, 1);
    }

    private static decimal CalculateCheckInScore(int totalCheckIns)
    {
        return totalCheckIns;
    }

    private static string FormatWeightLoss(decimal? start, decimal? current)
    {
        if (start is null or 0 || current is null)
            return "-";

        var loss = start.Value - current.Value;
        var percent = loss / start.Value * 100;
        return $"{loss:F1} kg ({percent:F1}%)";
    }
}
