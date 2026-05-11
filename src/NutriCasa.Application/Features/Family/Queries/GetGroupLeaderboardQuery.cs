using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.Family.Queries;

public record GetGroupLeaderboardQuery : IRequest<Result<GroupLeaderboardDto>>
{
    public string Category { get; init; } = "weight_loss";
}

public record GroupLeaderboardDto
{
    public string Category { get; init; } = "weight_loss";
    public string CategoryLabel { get; init; } = "";
    public List<LeaderboardEntryDto> Entries { get; init; } = new();
}

public record LeaderboardEntryDto
{
    public int Rank { get; init; }
    public Guid UserId { get; init; }
    public string FullName { get; init; } = "";
    public double Value { get; init; }
    public string ValueDisplay { get; init; } = "";
    public string Unit { get; init; } = "";
}

public class GetGroupLeaderboardQueryHandler : IRequestHandler<GetGroupLeaderboardQuery, Result<GroupLeaderboardDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetGroupLeaderboardQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<GroupLeaderboardDto>> Handle(GetGroupLeaderboardQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<GroupLeaderboardDto>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUser.UserId.Value;

        var membership = await _context.GroupMemberships
            .FirstOrDefaultAsync(m => m.UserId == userId && m.LeftAt == null, ct);

        if (membership is null)
            return Result<GroupLeaderboardDto>.Failure("No perteneces a ningún grupo.", "NO_GROUP");

        var groupUserIds = await _context.GroupMemberships
            .Where(m => m.GroupId == membership.GroupId && m.LeftAt == null)
            .Select(m => m.UserId)
            .ToListAsync(ct);

        if (groupUserIds.Count == 0)
            return Result<GroupLeaderboardDto>.Success(new GroupLeaderboardDto
            {
                Category = request.Category,
                CategoryLabel = GetCategoryLabel(request.Category),
            });

        var users = await _context.Users
            .Where(u => groupUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        var privacySettings = await _context.PrivacySettings
            .Where(p => groupUserIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, p => p, ct);

        var label = GetCategoryLabel(request.Category);

        var entries = new List<LeaderboardEntryDto>();

        switch (request.Category.ToLowerInvariant())
        {
            case "weight_loss":
                entries = await BuildWeightLossLeaderboard(groupUserIds, users, privacySettings, ct);
                break;
            case "streak":
                entries = await BuildStreakLeaderboard(groupUserIds, users, privacySettings, ct);
                break;
            case "adherence":
                entries = await BuildAdherenceLeaderboard(groupUserIds, users, privacySettings, ct);
                break;
            case "checkins":
                entries = await BuildCheckinLeaderboard(groupUserIds, users, privacySettings, ct);
                break;
        }

        var ranked = entries
            .OrderByDescending(e => e.Value)
            .Select((e, i) => e with { Rank = i + 1 })
            .ToList();

        return Result<GroupLeaderboardDto>.Success(new GroupLeaderboardDto
        {
            Category = request.Category,
            CategoryLabel = label,
            Entries = ranked,
        });
    }

    private static string GetCategoryLabel(string category) => category.ToLowerInvariant() switch
    {
        "weight_loss" => "Pérdida de peso",
        "streak" => "Racha de check-ins",
        "adherence" => "Adherencia",
        "checkins" => "Total check-ins",
        _ => category,
    };

    private async Task<List<LeaderboardEntryDto>> BuildWeightLossLeaderboard(
        List<Guid> groupUserIds,
        Dictionary<Guid, string> users,
        Dictionary<Guid, PrivacySettings> privacy,
        CancellationToken ct)
    {
        var allowed = groupUserIds
            .Where(id => privacy.TryGetValue(id, out var p) && p.ShareWeight >= VisibilityLevel.Group)
            .ToList();

        if (allowed.Count == 0) return new();

        var activeGoals = await _context.UserGoals
            .Where(g => allowed.Contains(g.UserId) && g.IsActive)
            .ToDictionaryAsync(g => g.UserId, ct);

        var latestWeights = await _context.BodyMeasurements
            .Where(m => allowed.Contains(m.UserId))
            .GroupBy(m => m.UserId)
            .Select(g => new { UserId = g.Key, Weight = g.OrderByDescending(m => m.MeasuredAt).ThenByDescending(m => m.CreatedAt).Select(m => (double)m.WeightKg).FirstOrDefault() })
            .ToDictionaryAsync(x => x.UserId, x => x.Weight, ct);

        var result = new List<LeaderboardEntryDto>();

        foreach (var uid in allowed)
        {
            if (!activeGoals.TryGetValue(uid, out var goal)) continue;
            if (!latestWeights.TryGetValue(uid, out var currentWeight) || currentWeight <= 0) continue;

            var startWeight = (double)goal.StartWeightKg;
            if (startWeight <= 0) continue;

            var lossPercent = Math.Round((startWeight - currentWeight) / startWeight * 100, 1);

            result.Add(new LeaderboardEntryDto
            {
                UserId = uid,
                FullName = users.GetValueOrDefault(uid, "Alguien"),
                Value = lossPercent,
                ValueDisplay = $"{lossPercent:0.0}%",
                Unit = "%",
            });
        }

        return result;
    }

    private async Task<List<LeaderboardEntryDto>> BuildStreakLeaderboard(
        List<Guid> groupUserIds,
        Dictionary<Guid, string> users,
        Dictionary<Guid, PrivacySettings> privacy,
        CancellationToken ct)
    {
        var allowed = groupUserIds
            .Where(id => privacy.TryGetValue(id, out var p) && p.ShareCheckIns >= VisibilityLevel.Group)
            .ToList();

        if (allowed.Count == 0) return new();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var allCheckins = await _context.DailyCheckIns
            .Where(c => allowed.Contains(c.UserId))
            .OrderByDescending(c => c.CheckInDate)
            .ToListAsync(ct);

        var grouped = allCheckins.GroupBy(c => c.UserId);
        var result = new List<LeaderboardEntryDto>();

        foreach (var uid in allowed)
        {
            var userCheckins = grouped
                .FirstOrDefault(g => g.Key == uid)?
                .OrderByDescending(c => c.CheckInDate)
                .ToList();

            if (userCheckins is null || userCheckins.Count == 0) continue;

            var streak = 0;
            var checkDate = today;
            foreach (var checkin in userCheckins)
            {
                if (checkin.CheckInDate == checkDate)
                {
                    streak++;
                    checkDate = checkDate.AddDays(-1);
                }
                else if (checkin.CheckInDate < checkDate)
                    break;
            }

            result.Add(new LeaderboardEntryDto
            {
                UserId = uid,
                FullName = users.GetValueOrDefault(uid, "Alguien"),
                Value = streak,
                ValueDisplay = $"{streak} día{(streak == 1 ? "" : "s")}",
                Unit = "días",
            });
        }

        return result;
    }

    private async Task<List<LeaderboardEntryDto>> BuildAdherenceLeaderboard(
        List<Guid> groupUserIds,
        Dictionary<Guid, string> users,
        Dictionary<Guid, PrivacySettings> privacy,
        CancellationToken ct)
    {
        var allowed = groupUserIds
            .Where(id => privacy.TryGetValue(id, out var p) && p.ShareCheckIns >= VisibilityLevel.Group)
            .ToList();

        if (allowed.Count == 0) return new();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var weekAgo = today.AddDays(-27);

        var recentCheckins = await _context.DailyCheckIns
            .Where(c => allowed.Contains(c.UserId) && c.CheckInDate >= weekAgo)
            .GroupBy(c => c.UserId)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), ct);

        var result = new List<LeaderboardEntryDto>();

        foreach (var uid in allowed)
        {
            var count = recentCheckins.GetValueOrDefault(uid, 0);
            var adherence = Math.Round((double)count / 28 * 100, 1);

            result.Add(new LeaderboardEntryDto
            {
                UserId = uid,
                FullName = users.GetValueOrDefault(uid, "Alguien"),
                Value = adherence,
                ValueDisplay = $"{adherence:0}%",
                Unit = "%",
            });
        }

        return result;
    }

    private async Task<List<LeaderboardEntryDto>> BuildCheckinLeaderboard(
        List<Guid> groupUserIds,
        Dictionary<Guid, string> users,
        Dictionary<Guid, PrivacySettings> privacy,
        CancellationToken ct)
    {
        var allowed = groupUserIds
            .Where(id => privacy.TryGetValue(id, out var p) && p.ShareCheckIns >= VisibilityLevel.Group)
            .ToList();

        if (allowed.Count == 0) return new();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthAgo = today.AddDays(-29);

        var checkinCounts = await _context.DailyCheckIns
            .Where(c => allowed.Contains(c.UserId) && c.CheckInDate >= monthAgo)
            .GroupBy(c => c.UserId)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), ct);

        var result = new List<LeaderboardEntryDto>();

        foreach (var uid in allowed)
        {
            var count = checkinCounts.GetValueOrDefault(uid, 0);

            result.Add(new LeaderboardEntryDto
            {
                UserId = uid,
                FullName = users.GetValueOrDefault(uid, "Alguien"),
                Value = count,
                ValueDisplay = $"{count} de 30",
                Unit = "check-ins",
            });
        }

        return result;
    }
}
