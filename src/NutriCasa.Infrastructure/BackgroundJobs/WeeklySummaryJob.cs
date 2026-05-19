using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Infrastructure.BackgroundJobs;

public class WeeklySummaryJob
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<WeeklySummaryJob> _logger;

    public WeeklySummaryJob(IApplicationDbContext context, ILogger<WeeklySummaryJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow;

        var daysSinceMonday = ((int)today.DayOfWeek - 1 + 7) % 7;
        var thisMonday = today.AddDays(-daysSinceMonday);

        var weekStart = DateOnly.FromDateTime(thisMonday.AddDays(-7));
        var weekEnd = DateOnly.FromDateTime(thisMonday.AddDays(-1));

        _logger.LogInformation("WeeklySummaryJob: generando resúmenes para semana {Start} a {End}", weekStart, weekEnd);

        var memberships = await _context.GroupMemberships
            .Include(m => m.User)
            .Where(m => m.LeftAt == null)
            .ToListAsync(ct);

        var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
        int created = 0, plateaus = 0;

        foreach (var userId in userIds)
        {
            try
            {
                var summary = await GenerateSummaryAsync(userId, weekStart, weekEnd, ct);
                if (summary is null) continue;

                var existing = await _context.WeeklySummaries
                    .AnyAsync(s => s.UserId == userId && s.WeekStartDate == weekStart, ct);

                if (existing) continue;

                _context.WeeklySummaries.Add(summary);
                created++;

                if (summary.IsInPlateau)
                {
                    await HandlePlateauAsync(userId, summary, memberships, ct);
                    plateaus++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando resumen semanal para usuario {UserId}", userId);
            }
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "WeeklySummaryJob: {Created} resúmenes creados, {Plateaus} mesetas detectadas.",
            created, plateaus);
    }

    private async Task<WeeklySummary?> GenerateSummaryAsync(
        Guid userId, DateOnly weekStart, DateOnly weekEnd, CancellationToken ct)
    {
        var checkIns = await _context.DailyCheckIns
            .Where(c => c.UserId == userId && c.CheckInDate >= weekStart && c.CheckInDate <= weekEnd)
            .ToListAsync(ct);

        if (checkIns.Count == 0)
            return null;

        var latestWeight = await _context.BodyMeasurements
            .Where(m => m.UserId == userId && m.MeasuredAt <= weekEnd)
            .OrderByDescending(m => m.MeasuredAt)
            .Select(m => (decimal?)m.WeightKg)
            .FirstOrDefaultAsync(ct);

        var previousWeight = await _context.BodyMeasurements
            .Where(m => m.UserId == userId && m.MeasuredAt < weekStart)
            .OrderByDescending(m => m.MeasuredAt)
            .Select(m => (decimal?)m.WeightKg)
            .FirstOrDefaultAsync(ct);

        decimal? weightChange = null;
        if (latestWeight.HasValue && previousWeight.HasValue)
            weightChange = Math.Round(latestWeight.Value - previousWeight.Value, 2);

        var plateau = await DetectPlateauAsync(userId, weekStart, weightChange, ct);

        return new WeeklySummary
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            WeekStartDate = weekStart,
            WeekEndDate = weekEnd,
            AvgDifficulty = checkIns.Any(c => c.DifficultyLevel.HasValue)
                ? Math.Round(checkIns.Where(c => c.DifficultyLevel.HasValue).Average(c => (decimal)c.DifficultyLevel!.Value), 2) : null,
            AvgHunger = checkIns.Any(c => c.HungerLevel.HasValue)
                ? Math.Round(checkIns.Where(c => c.HungerLevel.HasValue).Average(c => (decimal)c.HungerLevel!.Value), 2) : null,
            AvgEnergy = checkIns.Any(c => c.EnergyLevel.HasValue)
                ? Math.Round(checkIns.Where(c => c.EnergyLevel.HasValue).Average(c => (decimal)c.EnergyLevel!.Value), 2) : null,
            AdherencePercent = Math.Round((decimal)checkIns.Count / 7 * 100, 2),
            WeightChangeKg = weightChange,
            CheckInsCount = checkIns.Count,
            IsInPlateau = plateau,
            CreatedAt = DateTime.UtcNow,
        };
    }

    private async Task<bool> DetectPlateauAsync(
        Guid userId, DateOnly weekStart, decimal? currentWeightChange, CancellationToken ct)
    {
        if (currentWeightChange is null)
            return false;

        var recentSummaries = await _context.WeeklySummaries
            .Where(s => s.UserId == userId && s.WeekStartDate < weekStart && s.WeightChangeKg != null)
            .OrderByDescending(s => s.WeekStartDate)
            .Take(2)
            .ToListAsync(ct);

        var changes = recentSummaries
            .Select(s => Math.Abs(s.WeightChangeKg!.Value))
            .ToList();

        changes.Add(Math.Abs(currentWeightChange.Value));

        const decimal plateauThreshold = 0.5m;
        const int minPlateauWeeks = 3;

        if (changes.Count < minPlateauWeeks)
            return false;

        return changes.All(c => c < plateauThreshold);
    }

    private async Task HandlePlateauAsync(
        Guid userId, WeeklySummary summary, List<GroupMembership> memberships, CancellationToken ct)
    {
        var wasAlreadyInPlateau = await _context.WeeklySummaries
            .AnyAsync(s => s.UserId == userId && s.Id != summary.Id && s.IsInPlateau, ct);

        if (wasAlreadyInPlateau)
            return;

        var membership = memberships.FirstOrDefault(m => m.UserId == userId);
        if (membership is null) return;

        var post = new GroupPost
        {
            Id = Guid.NewGuid(),
            GroupId = membership.GroupId,
            AuthorUserId = null,
            PostType = PostType.AiMotivation,
            Content = GeneratePlateauMessage(),
            Metadata = "{\"type\":\"plateau_detected\",\"weekStart\":\"" + summary.WeekStartDate.ToString("yyyy-MM-dd") + "\"}",
            IsPinned = false,
            IsAnnouncement = false,
        };

        _context.GroupPosts.Add(post);

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = "plateau_detected",
            Priority = NotificationPriority.P2,
            Title = "Parece que estás en meseta",
            Body = "No te preocupes, es normal. Hemos publicado un mensaje de motivación en el muro de tu grupo. ¡No te rindas!",
            DeepLink = "/app/group",
            DeliveryChannels = ["in_app", "push"],
            CreatedAt = DateTime.UtcNow,
        };

        _context.Notifications.Add(notification);
    }

    private static string GeneratePlateauMessage()
    {
        var messages = new[]
        {
            "🌈 ¡Ánimo! Las mesetas son parte del proceso. El cuerpo necesita ajustarse antes de seguir perdiendo grasa. Sigue constante, ¡los resultados llegarán!",
            "💪 No es un estancamiento, es una pausa estratégica. Tu cuerpo se está reacomodando. Confía en el proceso y sigue dando lo mejor de ti.",
            "🌱 Las mesetas son señal de que estás construyendo hábitos sólidos. El peso no siempre cuenta la historia completa. ¡Mide tus logros en energía, fuerza y bienestar!",
            "🔥 Cuando las cosas se ponen difíciles, es cuando ocurre la magia. No dejes que una meseta borre todo el progreso que ya has logrado.",
            "🎯 El progreso no siempre es lineal. Hay subidas y bajadas, pero mientras sigas moviéndote hacia adelante, estás ganando. ¡Sigue así!",
        };

        return messages[Random.Shared.Next(messages.Length)];
    }
}
