using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NutriCasa.Application.Common.Interfaces;

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

            if (stats.AvgDifficulty >= 8)
                recalibratedCount++;

            if (stats.AvgHunger >= 8)
                recalibratedCount++;

            if (stats.AvgEnergy <= 3)
            {
                _logger.LogWarning("Usuario {UserId}: energía baja detectada ({AvgEnergy}), sugerir electrolitos.",
                    stats.UserId, stats.AvgEnergy);
            }
        }

        _logger.LogInformation(
            "WeeklyDifficultyAnalysis: {Total} usuarios analizados, {Recalibrated} necesitan recalibración.",
            usersWithCheckIns.Count, recalibratedCount);
    }
}
