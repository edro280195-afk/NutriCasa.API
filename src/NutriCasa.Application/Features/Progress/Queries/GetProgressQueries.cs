using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Progress.DTOs;

namespace NutriCasa.Application.Features.Progress.Queries;

public record GetProgressSummaryQuery : IRequest<Result<ProgressSummaryDto>>;

public record GetWeightHistoryQuery : IRequest<Result<List<WeightEntryDto>>>;

public record GetCheckinHeatmapQuery : IRequest<Result<List<CheckinDayDto>>>;

public record GetWeeklyMacrosQuery : IRequest<Result<WeeklyMacrosDto>>;

public class GetProgressSummaryQueryHandler : IRequestHandler<GetProgressSummaryQuery, Result<ProgressSummaryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetProgressSummaryQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ProgressSummaryDto>> Handle(GetProgressSummaryQuery request, CancellationToken ct)
    {
        if (_currentUserService.UserId is null)
            return Result<ProgressSummaryDto>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var activeGoal = await _context.UserGoals
            .FirstOrDefaultAsync(g => g.UserId == userId && g.IsActive, ct);

        var latestWeight = await _context.BodyMeasurements
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.MeasuredAt)
            .ThenByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var allCheckins = await _context.DailyCheckIns
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CheckInDate)
            .ToListAsync(ct);

        var streakDays = 0;
        var checkDate = today;
        foreach (var checkin in allCheckins)
        {
            if (checkin.CheckInDate == checkDate)
            {
                streakDays++;
                checkDate = checkDate.AddDays(-1);
            }
            else if (checkin.CheckInDate < checkDate)
            {
                break;
            }
        }

        var weekAgo = today.AddDays(-6);
        var weeklyCheckins = allCheckins.Count(c => c.CheckInDate >= weekAgo);

        var startDate = activeGoal?.CreatedAt ?? DateTime.UtcNow;
        var daysSinceStart = (DateTime.UtcNow - startDate).Days + 1;
        var totalCheckins = allCheckins.Count;
        var overallAdherence = daysSinceStart > 0
            ? Math.Round((double)totalCheckins / daysSinceStart * 100, 1)
            : 0;

        var dto = new ProgressSummaryDto
        {
            CurrentWeight = latestWeight != null ? (double)latestWeight.WeightKg : 0,
            StartWeight = activeGoal != null ? (double)activeGoal.StartWeightKg : 0,
            GoalWeight = activeGoal?.TargetWeightKg != null ? (double)activeGoal.TargetWeightKg.Value : 0,
            WeightChange = latestWeight != null && activeGoal != null
                ? Math.Round((double)latestWeight.WeightKg - (double)activeGoal.StartWeightKg, 1)
                : 0,
            StreakDays = streakDays,
            WeeklyAdherence = Math.Round((double)weeklyCheckins / 7 * 100, 1),
            OverallAdherence = overallAdherence,
            StartDate = startDate.ToString("d MMMM", new System.Globalization.CultureInfo("es-MX")),
            CheckinsCompleted = weeklyCheckins,
            TotalCheckins = 7,
        };

        return Result<ProgressSummaryDto>.Success(dto);
    }
}

public class GetWeightHistoryQueryHandler : IRequestHandler<GetWeightHistoryQuery, Result<List<WeightEntryDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetWeightHistoryQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<WeightEntryDto>>> Handle(GetWeightHistoryQuery request, CancellationToken ct)
    {
        if (_currentUserService.UserId is null)
            return Result<List<WeightEntryDto>>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var measurements = await _context.BodyMeasurements
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.MeasuredAt)
            .ThenBy(m => m.CreatedAt)
            .Take(14)
            .ToListAsync(ct);

        if (measurements.Count == 0)
            return Result<List<WeightEntryDto>>.Success([]);

        var minWeight = measurements.Min(m => m.WeightKg);
        var maxWeight = measurements.Max(m => m.WeightKg);
        var range = maxWeight - minWeight;
        if (range == 0) range = 1;

        var colors = new[] { "var(--mint)", "var(--mint-light)", "var(--lake)" };
        var colorIndex = 0;

        var entries = measurements.Select(m =>
        {
            var height = 40 + (double)((maxWeight - m.WeightKg) / range) * 60;
            var color = colors[colorIndex % colors.Length];
            colorIndex++;

            return new WeightEntryDto
            {
                Date = m.MeasuredAt.ToString("d MMM", new System.Globalization.CultureInfo("es-MX")),
                WeightKg = (double)m.WeightKg,
                HeightPercent = Math.Round(height, 1),
                Color = color,
            };
        }).ToList();

        return Result<List<WeightEntryDto>>.Success(entries);
    }
}

public class GetCheckinHeatmapQueryHandler : IRequestHandler<GetCheckinHeatmapQuery, Result<List<CheckinDayDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCheckinHeatmapQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<CheckinDayDto>>> Handle(GetCheckinHeatmapQuery request, CancellationToken ct)
    {
        if (_currentUserService.UserId is null)
            return Result<List<CheckinDayDto>>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var daysAgo = today.AddDays(-83); // 12 semanas = 84 días

        var checkins = await _context.DailyCheckIns
            .Where(c => c.UserId == userId && c.CheckInDate >= daysAgo)
            .ToListAsync(ct);

        var checkinByDate = checkins.ToDictionary(c => c.CheckInDate);

        var days = new List<CheckinDayDto>();
        for (var date = daysAgo; date <= today; date = date.AddDays(1))
        {
            var level = 0;
            if (checkinByDate.TryGetValue(date, out var checkin))
            {
                if (checkin.DifficultyLevel.HasValue && checkin.DifficultyLevel >= 7)
                    level = 3;
                else if (checkin.HungerLevel.HasValue && checkin.HungerLevel <= 3)
                    level = 3;
                else if (checkin.EnergyLevel.HasValue && checkin.EnergyLevel >= 7)
                    level = 3;
                else if (checkin.MoodLevel.HasValue && checkin.MoodLevel >= 7)
                    level = 2;
                else
                    level = 1;
            }

            days.Add(new CheckinDayDto
            {
                Date = date.ToString("ddd", new System.Globalization.CultureInfo("es-MX")),
                Level = level,
            });
        }

        return Result<List<CheckinDayDto>>.Success(days);
    }
}

public class GetWeeklyMacrosQueryHandler : IRequestHandler<GetWeeklyMacrosQuery, Result<WeeklyMacrosDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetWeeklyMacrosQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<WeeklyMacrosDto>> Handle(GetWeeklyMacrosQuery request, CancellationToken ct)
    {
        if (_currentUserService.UserId is null)
            return Result<WeeklyMacrosDto>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var ketoProfile = await _context.KetoProfiles
            .FirstOrDefaultAsync(k => k.UserId == userId, ct);

        var goalCalories = ketoProfile?.DailyCalories ?? 2000;
        var goalProtein = (double)(ketoProfile?.ProteinGrams ?? 0);
        var goalFat = (double)(ketoProfile?.FatGrams ?? 0);
        var goalCarbs = (double)(ketoProfile?.CarbsGrams ?? 0);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var activePlan = await _context.WeeklyPlans
            .Include(p => p.Meals)
            .ThenInclude(m => m.Recipe)
            .FirstOrDefaultAsync(p =>
                p.UserId == userId &&
                p.IsActive &&
                p.StartDate <= today &&
                p.EndDate >= today, ct);

        double currentCalories = 0, currentProtein = 0, currentFat = 0, currentCarbs = 0;

        if (activePlan?.Meals != null && activePlan.Meals.Count > 0)
        {
            var mealsWithRecipes = activePlan.Meals
                .Where(m => m.Recipe != null)
                .ToList();

            if (mealsWithRecipes.Count > 0)
            {
                currentCalories = mealsWithRecipes.Sum(m => (double)(m.Recipe!.BaseCalories * m.PortionMultiplier));
                currentProtein = Math.Round(mealsWithRecipes.Sum(m => (double)(m.Recipe!.BaseProteinGr * m.PortionMultiplier)), 1);
                currentFat = Math.Round(mealsWithRecipes.Sum(m => (double)(m.Recipe!.BaseFatGr * m.PortionMultiplier)), 1);
                currentCarbs = Math.Round(mealsWithRecipes.Sum(m => (double)(m.Recipe!.BaseCarbsGr * m.PortionMultiplier)), 1);
            }
        }

        var dto = new WeeklyMacrosDto
        {
            Calories = new MacroGoalDto { Current = currentCalories, Goal = goalCalories },
            Protein = new MacroGoalDto { Current = currentProtein, Goal = goalProtein },
            Fat = new MacroGoalDto { Current = currentFat, Goal = goalFat },
            Carbs = new MacroGoalDto { Current = currentCarbs, Goal = goalCarbs },
        };

        return Result<WeeklyMacrosDto>.Success(dto);
    }
}
