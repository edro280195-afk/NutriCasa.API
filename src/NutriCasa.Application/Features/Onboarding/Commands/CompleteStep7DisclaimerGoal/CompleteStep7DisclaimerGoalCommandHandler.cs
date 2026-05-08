using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Onboarding.DTOs;
using NutriCasa.Application.Services;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.Onboarding.Commands.CompleteStep7DisclaimerGoal;

public class CompleteStep7DisclaimerGoalCommandHandler : IRequestHandler<CompleteStep7DisclaimerGoalCommand, Result<CompleteStep7DisclaimerGoalResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly KetoProfileCalculator _ketoCalculator;

    public CompleteStep7DisclaimerGoalCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        KetoProfileCalculator ketoCalculator)
    {
        _context = context;
        _currentUserService = currentUserService;
        _ketoCalculator = ketoCalculator;
    }

    public async Task<Result<CompleteStep7DisclaimerGoalResponse>> Handle(CompleteStep7DisclaimerGoalCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
            return Result<CompleteStep7DisclaimerGoalResponse>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var user = await _context.Users
            .Include(u => u.UserGoals.Where(g => g.IsActive))
            .Include(u => u.MedicalProfile)
            .Include(u => u.KetoProfile)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return Result<CompleteStep7DisclaimerGoalResponse>.Failure("Usuario no encontrado.", "NOT_FOUND");

        DisclaimerVersion? disclaimer;
        if (request.DisclaimerVersionId.HasValue)
        {
            disclaimer = await _context.DisclaimerVersions
                .FirstOrDefaultAsync(d => d.Id == request.DisclaimerVersionId.Value, cancellationToken);
        }
        else
        {
            disclaimer = await _context.DisclaimerVersions
                .FirstOrDefaultAsync(d => d.DisclaimerType == "general" && d.IsCurrent, cancellationToken);
        }

        if (disclaimer is null)
            return Result<CompleteStep7DisclaimerGoalResponse>.Failure(
                "El disclaimer general no está disponible.", "INVALID_DISCLAIMER");

        // 1. Actualizar disclaimer en usuario
        user.DisclaimerAcceptedAt = DateTime.UtcNow;
        user.DisclaimerVersionId = request.DisclaimerVersionId;

        // 2. Actualizar meta activa si existe
        var activeGoal = user.UserGoals.FirstOrDefault(g => g.IsActive);
        if (activeGoal is not null)
        {
            activeGoal.TargetWeightKg = request.TargetWeightKg ?? activeGoal.TargetWeightKg;
            activeGoal.GoalType = Enum.Parse<GoalType>(request.GoalType);
            if (request.TargetDate.HasValue)
                activeGoal.TargetDate = request.TargetDate.Value;
            activeGoal.MotivationText = request.MotivationText;
        }

        // 3. Leer thresholds para keto calculator
        var bmrFloorFactor = await GetThresholdDecimalAsync("bmr_calorie_floor_factor", 0.85m, cancellationToken);
        var tdeeCeilingFactor = await GetThresholdDecimalAsync("tdee_calorie_ceiling_factor", 1.10m, cancellationToken);
        var minProteinPerKg = await GetThresholdDecimalAsync("minimum_protein_per_kg", 0.8m, cancellationToken);
        var overrideMaxCarbs = await GetThresholdIntAsync("plan_carbs_max_grams_override", 60, cancellationToken);

        // 4. Obtener modo de presupuesto para proteína mínima por modo
        decimal? budgetModeMinProtein = null;
        if (user.BudgetModeId.HasValue)
        {
            var budgetMode = await _context.BudgetModes
                .FirstOrDefaultAsync(b => b.Id == user.BudgetModeId, cancellationToken);
            if (budgetMode?.Code == "athletic")
                budgetModeMinProtein = 1.6m;
        }

        // 5. Calcular edad
        int age = DateTime.UtcNow.Year - user.BirthDate.Year;
        if (user.BirthDate > DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-age))) age--;

        bool isOverride = user.MedicalProfile?.RequiresHumanReview == true
                          && user.MedicalProfile?.OverrideAcceptedAt != null;

        // 6. Calcular keto profile
        var result = _ketoCalculator.Calculate(
            weightKg: activeGoal?.StartWeightKg ?? 70m,
            heightCm: user.HeightCm > 0 ? user.HeightCm : 160m,
            age: age,
            gender: user.Gender,
            activityLevel: user.ActivityLevel,
            goalType: Enum.Parse<GoalType>(request.GoalType),
            bmrCalorieFloorFactor: bmrFloorFactor,
            tdeeCalorieCeilingFactor: tdeeCeilingFactor,
            minimumProteinPerKg: minProteinPerKg,
            budgetModeMinProteinPerKg: budgetModeMinProtein,
            isOverridePlan: isOverride,
            overrideMaxCarbsGrams: overrideMaxCarbs);

        // 7. Guardar keto_profile
        if (user.KetoProfile is not null)
        {
            user.KetoProfile.BmrKcal = (int?)result.BmrKcal;
            user.KetoProfile.TdeeKcal = (int?)result.TdeeKcal;
            user.KetoProfile.DailyCalories = result.DailyCalories;
            user.KetoProfile.CarbsGrams = result.CarbsGrams;
            user.KetoProfile.ProteinGrams = result.ProteinGrams;
            user.KetoProfile.FatGrams = result.FatGrams;
            user.KetoProfile.CarbsPercent = result.CarbsPercent;
            user.KetoProfile.ProteinPercent = result.ProteinPercent;
            user.KetoProfile.FatPercent = result.FatPercent;
            user.KetoProfile.LastCalculatedAt = DateTime.UtcNow;
        }
        else
        {
            var ketoProfile = new KetoProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                BmrKcal = (int?)result.BmrKcal,
                TdeeKcal = (int?)result.TdeeKcal,
                DailyCalories = result.DailyCalories,
                CarbsGrams = result.CarbsGrams,
                ProteinGrams = result.ProteinGrams,
                FatGrams = result.FatGrams,
                CarbsPercent = result.CarbsPercent,
                ProteinPercent = result.ProteinPercent,
                FatPercent = result.FatPercent,
                LastCalculatedAt = DateTime.UtcNow,
            };
            _context.KetoProfiles.Add(ketoProfile);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<CompleteStep7DisclaimerGoalResponse>.Success(new CompleteStep7DisclaimerGoalResponse
        {
            OnboardingComplete = true,
            KetoProfile = result
        });
    }

    private async Task<decimal> GetThresholdDecimalAsync(string code, decimal def, CancellationToken ct)
    {
        var t = await _context.SystemThresholds
            .FirstOrDefaultAsync(x => x.Code == code && x.IsActive, ct);
        return t?.NumericValue ?? def;
    }

    private async Task<int> GetThresholdIntAsync(string code, int def, CancellationToken ct)
    {
        var t = await _context.SystemThresholds
            .FirstOrDefaultAsync(x => x.Code == code && x.IsActive, ct);
        return (int)(t?.NumericValue ?? def);
    }
}
