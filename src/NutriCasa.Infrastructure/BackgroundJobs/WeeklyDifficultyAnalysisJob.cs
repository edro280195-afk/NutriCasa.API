using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Infrastructure.BackgroundJobs;

public class WeeklyDifficultyAnalysisJob
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<WeeklyDifficultyAnalysisJob> _logger;

    public WeeklyDifficultyAnalysisJob(
        IApplicationDbContext context,
        ILogger<WeeklyDifficultyAnalysisJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        var usersWithCheckIns = await _context.DailyCheckIns
            .Where(c => c.CheckInDate >= DateOnly.FromDateTime(sevenDaysAgo))
            .GroupBy(c => c.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                AvgDifficulty = g.Average(c => (double?)c.DifficultyLevel),
                AvgHunger = g.Average(c => (double?)c.HungerLevel),
                AvgEnergy = g.Average(c => (double?)c.EnergyLevel),
                CheckInCount = g.Count()
            })
            .ToListAsync(ct);

        int recalibratedCount = 0;

        foreach (var stats in usersWithCheckIns)
        {
            if (stats.CheckInCount < 4) continue;

            // 1. Evaluar dificultad/hambre alta para suavizar la dificultad
            bool needsSoften = (stats.AvgDifficulty >= 8) || (stats.AvgHunger >= 8);
            if (needsSoften)
            {
                var profile = await _context.KetoProfiles.FirstOrDefaultAsync(p => p.UserId == stats.UserId, ct);
                if (profile != null)
                {
                    int newCalories = (int)Math.Round(profile.DailyCalories * 1.10);
                    if (profile.TdeeKcal.HasValue && profile.TdeeKcal.Value > 0 && newCalories > profile.TdeeKcal.Value)
                    {
                        newCalories = profile.TdeeKcal.Value;
                    }

                    if (newCalories > profile.DailyCalories)
                    {
                        profile.DailyCalories = newCalories;
                        profile.CarbsGrams = Math.Round((newCalories * 0.05m) / 4m, 1);
                        profile.ProteinGrams = Math.Round((newCalories * 0.25m) / 4m, 1);
                        profile.FatGrams = Math.Round((newCalories - (profile.CarbsGrams * 4m) - (profile.ProteinGrams * 4m)) / 9m, 1);
                        profile.LastCalculatedAt = DateTime.UtcNow;
                        profile.CalculationMethod += "_softened";

                        _context.KetoProfiles.Update(profile);
                        recalibratedCount++;

                        var notification = new Notification
                        {
                            UserId = stats.UserId,
                            Type = "difficulty_recalibrated",
                            Priority = NotificationPriority.P2,
                            Title = "Meta calórica ajustada",
                            Body = "Notamos que esta semana fue muy difícil o tuviste mucha hambre. Hemos suavizado la meta de calorías de tu plan de alimentación para que sea más llevadero. ¡Tu bienestar es lo primero!",
                            DeepLink = "/app/plan",
                            DeliveryChannels = ["in_app", "push"],
                            CreatedAt = DateTime.UtcNow,
                            SentAt = DateTime.UtcNow
                        };
                        _context.Notifications.Add(notification);
                    }
                }
            }

            // 2. Detección de mesetas y sugerencia de refeed
            var lastSummaries = await _context.WeeklySummaries
                .Where(s => s.UserId == stats.UserId)
                .OrderByDescending(s => s.WeekStartDate)
                .Take(4)
                .ToListAsync(ct);

            bool isPlateau = lastSummaries.Count >= 4 && lastSummaries.All(s => s.WeightChangeKg.HasValue && Math.Abs(s.WeightChangeKg.Value) < 0.5m);

            if (isPlateau)
            {
                var latestSummary = lastSummaries.First();
                if (!latestSummary.IsInPlateau)
                {
                    latestSummary.IsInPlateau = true;
                    _context.WeeklySummaries.Update(latestSummary);
                }

                var activeGoal = await _context.UserGoals
                    .Where(g => g.UserId == stats.UserId && g.IsActive && g.GoalType == GoalType.WeightLoss)
                    .FirstOrDefaultAsync(ct);

                if (activeGoal != null && activeGoal.CreatedAt <= DateTime.UtcNow.AddDays(-56))
                {
                    var alreadySent = await _context.Notifications
                        .AnyAsync(n => n.UserId == stats.UserId && n.Type == "refeed_suggested" && n.CreatedAt >= DateTime.UtcNow.AddDays(-7), ct);

                    if (!alreadySent)
                    {
                        var notification = new Notification
                        {
                            UserId = stats.UserId,
                            Type = "refeed_suggested",
                            Priority = NotificationPriority.P2,
                            Title = "Sugerencia de Refeed",
                            Body = "¡Hola! Has estado en déficit calórico durante 8 semanas y tu pérdida de peso se ha estabilizado. Te sugerimos realizar una semana de 'refeed' (comer en tus calorías de mantenimiento) para reactivar tu metabolismo y romper la meseta.",
                            DeepLink = "/app/plan",
                            DeliveryChannels = ["in_app", "push"],
                            CreatedAt = DateTime.UtcNow,
                            SentAt = DateTime.UtcNow
                        };
                        _context.Notifications.Add(notification);
                    }
                }
            }

            if (stats.AvgEnergy <= 3)
            {
                _logger.LogWarning("Usuario {UserId}: energía baja detectada ({AvgEnergy}), sugerir electrolitos.",
                    stats.UserId, stats.AvgEnergy);
            }
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "WeeklyDifficultyAnalysis: {Total} usuarios analizados, {Recalibrated} fueron recalibrados.",
            usersWithCheckIns.Count, recalibratedCount);
    }
}
