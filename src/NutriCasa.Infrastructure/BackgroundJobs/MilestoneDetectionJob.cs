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
                };
                _context.UserMilestones.Add(milestone);
                detectedCount++;

                if (await PostToGroupWallAsync(user, $"{user.FullName} ya perdió 5 kg 🥳 ¡Impresionante dedicación!", ct))
                    postCount++;
            }

            if (totalLost >= 10 && !existingMilestones.Contains("ten_kg"))
            {
                var milestone = new UserMilestone
                {
                    UserId = user.Id,
                    MilestoneType = "ten_kg",
                    MilestoneValue = (decimal)totalLost,
                    AchievedAt = DateTime.UtcNow,
                };
                _context.UserMilestones.Add(milestone);
                detectedCount++;

                if (await PostToGroupWallAsync(user, $"{user.FullName} perdió 10 kg 🏆 ¡Logro extraordinario!", ct))
                    postCount++;
            }

            var checkInDates = await _context.DailyCheckIns
                .Where(c => c.UserId == user.Id)
                .OrderByDescending(c => c.CheckInDate)
                .Select(c => c.CheckInDate)
                .ToListAsync(ct);

            var currentStreak = CalculateCurrentStreak(checkInDates);

            if (currentStreak >= 7 && !existingMilestones.Contains("week_streak"))
            {
                var milestone = new UserMilestone
                {
                    UserId = user.Id,
                    MilestoneType = "week_streak",
                    MilestoneValue = currentStreak,
                    AchievedAt = DateTime.UtcNow,
                };
                _context.UserMilestones.Add(milestone);
                detectedCount++;

                if (await PostToGroupWallAsync(user, $"{user.FullName} completó {currentStreak} días de check-in consecutivos 🔥", ct))
                    postCount++;
            }

            if (currentStreak >= 30 && !existingMilestones.Contains("thirty_day"))
            {
                var milestone = new UserMilestone
                {
                    UserId = user.Id,
                    MilestoneType = "thirty_day",
                    MilestoneValue = currentStreak,
                    AchievedAt = DateTime.UtcNow,
                };
                _context.UserMilestones.Add(milestone);
                detectedCount++;

                if (await PostToGroupWallAsync(user, $"{user.FullName} lleva {currentStreak} días consecutivos de check-in 💪 ¡Un mes completo!", ct))
                    postCount++;
            }

            if (currentStreak >= 60 && !existingMilestones.Contains("sixty_day"))
            {
                var milestone = new UserMilestone
                {
                    UserId = user.Id,
                    MilestoneType = "sixty_day",
                    MilestoneValue = currentStreak,
                    AchievedAt = DateTime.UtcNow,
                };
                _context.UserMilestones.Add(milestone);
                detectedCount++;

                if (await PostToGroupWallAsync(user, $"{user.FullName} alcanzó {currentStreak} días de check-in consecutivos 👑 ¡Dos meses imparable!", ct))
                    postCount++;
            }
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("MilestoneDetection: {Detected} hitos detectados, {Posted} publicados al muro.", detectedCount, postCount);
    }

    private static int CalculateCurrentStreak(List<DateOnly> checkInDates)
    {
        if (checkInDates.Count == 0) return 0;

        checkInDates = checkInDates.OrderByDescending(d => d).ToList();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if ((today.DayNumber - checkInDates[0].DayNumber) > 1)
            return 0;

        int streak = 1;
        for (int i = 1; i < checkInDates.Count; i++)
        {
            if ((checkInDates[i - 1].DayNumber - checkInDates[i].DayNumber) == 1)
                streak++;
            else
                break;
        }

        return streak;
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
