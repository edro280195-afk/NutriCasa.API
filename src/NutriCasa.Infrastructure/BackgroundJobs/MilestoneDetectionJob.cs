using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Infrastructure.BackgroundJobs;

public class MilestoneDetectionJob
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<MilestoneDetectionJob> _logger;

    public MilestoneDetectionJob(
        IApplicationDbContext context,
        ILogger<MilestoneDetectionJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var detectedCount = 0;
        var postCount = 0;

        var users = await _context.Users
            .Include(u => u.UserGoals)
            .Where(u => u.DeletedAt == null)
            .ToListAsync(ct);

        foreach (var user in users)
        {
            var firstMeasurement = await _context.BodyMeasurements
                .Where(m => m.UserId == user.Id)
                .OrderBy(m => m.MeasuredAt)
                .FirstOrDefaultAsync(ct);

            if (firstMeasurement is null) continue;

            var currentWeight = await _context.BodyMeasurements
                .Where(m => m.UserId == user.Id)
                .OrderByDescending(m => m.MeasuredAt)
                .Select(m => m.WeightKg)
                .FirstOrDefaultAsync(ct);

            var totalLost = firstMeasurement.WeightKg - currentWeight;

            var existingMilestones = await _context.UserMilestones
                .Where(m => m.UserId == user.Id)
                .Select(m => m.MilestoneType)
                .ToListAsync(ct);

            if (totalLost >= 1 && !existingMilestones.Contains("first_kg"))
            {
                var milestone = new UserMilestone
                {
                    UserId = user.Id,
                    MilestoneType = "first_kg",
                    MilestoneValue = (decimal)totalLost,
                    AchievedAt = DateTime.UtcNow,
                    PostedToGroup = false,
                };
                _context.UserMilestones.Add(milestone);
                detectedCount++;

                if (await PostToGroupWallAsync(user, $"{user.FullName} alcanzó su primer kilo perdido 🎉 ¡{totalLost:F1} kg menos!", ct))
                    postCount++;
            }

            if (totalLost >= 5 && !existingMilestones.Contains("five_kg"))
            {
                var milestone = new UserMilestone
                {
                    UserId = user.Id,
                    MilestoneType = "five_kg",
                    MilestoneValue = (decimal)totalLost,
                    AchievedAt = DateTime.UtcNow,
                    PostedToGroup = false,
                };
                _context.UserMilestones.Add(milestone);
                detectedCount++;

                if (await PostToGroupWallAsync(user, $"{user.FullName} ya perdió 5 kg 🥳 ¡Impresionante dedicación!", ct))
                    postCount++;
            }

            var weekStreak = await _context.DailyCheckIns
                .Where(c => c.UserId == user.Id)
                .OrderByDescending(c => c.CheckInDate)
                .Take(30)
                .CountAsync(ct);

            if (weekStreak >= 7 && !existingMilestones.Contains("week_streak"))
            {
                var milestone = new UserMilestone
                {
                    UserId = user.Id,
                    MilestoneType = "week_streak",
                    MilestoneValue = weekStreak,
                    AchievedAt = DateTime.UtcNow,
                    PostedToGroup = false,
                };
                _context.UserMilestones.Add(milestone);
                detectedCount++;

                if (await PostToGroupWallAsync(user, $"{user.FullName} completó {weekStreak} días de check-in consecutivos 🔥", ct))
                    postCount++;
            }
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("MilestoneDetection: {Detected} hitos detectados, {Posted} publicados al muro.", detectedCount, postCount);
    }

    private async Task<bool> PostToGroupWallAsync(User user, string content, CancellationToken ct)
    {
        try
        {
            var membership = await _context.GroupMemberships
                .Include(m => m.Group)
                .FirstOrDefaultAsync(m => m.UserId == user.Id && m.LeftAt == null, ct);

            if (membership?.Group is null) return false;

            var post = new GroupPost
            {
                Id = Guid.NewGuid(),
                GroupId = membership.GroupId,
                AuthorUserId = null,
                PostType = PostType.Milestone,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            _context.GroupPosts.Add(post);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al publicar hito al muro del usuario {UserId}", user.Id);
            return false;
        }
    }
}
