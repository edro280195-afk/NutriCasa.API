using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Domain.Constants;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Infrastructure.BackgroundJobs;

public class RefeedDetectionJob
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<RefeedDetectionJob> _logger;

    public RefeedDetectionJob(IApplicationDbContext context, ILogger<RefeedDetectionJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var featureFlag = await _context.FeatureFlags
            .FirstOrDefaultAsync(f => f.Code == "refeed_diet_break" && f.IsEnabled, ct);

        if (featureFlag is null)
        {
            _logger.LogInformation("RefeedDetectionJob: feature flag 'refeed_diet_break' deshabilitado.");
            return;
        }

        var thresholdWeeks = await _context.SystemThresholds
            .Where(t => t.Code == SystemThresholdCodes.WeeksContinuousDeficitForRefeed && t.IsActive)
            .Select(t => (int?)(t.NumericValue ?? 8))
            .FirstOrDefaultAsync(ct) ?? 8;

        var memberships = await _context.GroupMemberships
            .Include(m => m.User)
            .Where(m => m.LeftAt == null)
            .ToListAsync(ct);

        var userIds = memberships.Select(m => m.UserId).Distinct().ToList();
        int suggested = 0;

        foreach (var userId in userIds)
        {
            try
            {
                var shouldSuggest = await ShouldSuggestRefeedAsync(userId, thresholdWeeks, ct);
                if (!shouldSuggest) continue;

                var membership = memberships.FirstOrDefault(m => m.UserId == userId);
                if (membership is null) continue;

                await CreateRefeedSuggestionAsync(userId, membership.GroupId, ct);
                suggested++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revisando refeed para usuario {UserId}", userId);
            }
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("RefeedDetectionJob: {Suggested} sugerencias de refeed generadas.", suggested);
    }

    private async Task<bool> ShouldSuggestRefeedAsync(Guid userId, int thresholdWeeks, CancellationToken ct)
    {
        var summaries = await _context.WeeklySummaries
            .Where(s => s.UserId == userId && s.AdherencePercent != null)
            .OrderByDescending(s => s.WeekStartDate)
            .Take(thresholdWeeks + 4)
            .ToListAsync(ct);

        if (summaries.Count < thresholdWeeks)
            return false;

        int consecutive = 0;
        foreach (var s in summaries)
        {
            if (s.AdherencePercent >= 70)
            {
                consecutive++;
                if (consecutive >= thresholdWeeks)
                    break;
            }
            else
            {
                consecutive = 0;
            }
        }

        if (consecutive < thresholdWeeks)
            return false;

        var alreadyNotified = await _context.Notifications
            .AnyAsync(n => n.UserId == userId && n.Type == "refeed_suggestion"
                        && n.CreatedAt >= DateTime.UtcNow.AddDays(-14), ct);

        if (alreadyNotified)
            return false;

        return true;
    }

    private async Task CreateRefeedSuggestionAsync(Guid userId, Guid groupId, CancellationToken ct)
    {
        var post = new GroupPost
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            AuthorUserId = null,
            PostType = PostType.AiMotivation,
            Content = GenerateRefeedMessage(),
            Metadata = "{\"type\":\"refeed_suggestion\"}",
            IsPinned = false,
            IsAnnouncement = false,
        };

        _context.GroupPosts.Add(post);

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = "refeed_suggestion",
            Priority = NotificationPriority.P2,
            Title = "¡Semana de recarga sugerida! 🚀",
            Body = "Has tenido una racha increíble de constancia. Tu cuerpo podría beneficiarse de una semana de recarga (refeed). Revisa el muro de tu grupo para más info.",
            DeepLink = "/app/group",
            DeliveryChannels = ["in_app", "push"],
            CreatedAt = DateTime.UtcNow,
        };

        _context.Notifications.Add(notification);
    }

    private static string GenerateRefeedMessage()
    {
        var messages = new[]
        {
            "🚀 ¡Vas increíble! Después de tantas semanas de déficit constante, una semana de recarga (refeed) puede ayudarte a resetear hormonas, recuperar energía y evitar estancamientos. Sube tus carbos a mantenimiento por una semana — tu cuerpo te lo agradecerá.",
            "⚡ Semana de recarga detectada. Llevas semanas siendo constante — ¿qué tal si le das un respiro a tu metabolismo con una semana a mantenimiento? Más energía, mejor rendimiento y menos estrés metabólico.",
            "🔥 Constancia no es lo mismo que rigidez. Después de semanas en déficit, una semana de recarga puede reactivar tu metabolismo y hacer que sigas perdiendo grasa más fácilmente después. ¡No le temas a los carbos!",
            "🎯 Estás haciendo un trabajo excepcional. Una semana de recarga estratégica cada 8-12 semanas puede mejorar tu adherencia a largo plazo y darle a tu cuerpo lo que necesita para seguir rindiendo al máximo.",
            "💡 ¿Sabías que los atletas y fisicoculturistas usan semanas de recarga para optimizar su composición corporal? No es trampa, es estrategia. Dale a tu cuerpo una semana a mantenimiento y vuelve más fuerte."
        };

        return messages[Random.Shared.Next(messages.Length)];
    }
}
