using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.Plans.Commands.GeneratePlan;

public record PlanGenerationResult
{
    public required Guid PlanId { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public required string BudgetModeCode { get; init; }
    public required string BudgetModeName { get; init; }
    public required bool IsOverridePlan { get; init; }
    public decimal? EstimatedCostMxn { get; init; }
    public decimal? SavingsVsGourmetMxn { get; init; }
    public decimal? SavingsVsGourmetPercent { get; init; }
    public required List<DayPlanDto> Days { get; init; }
    public required KetoProfileResult Macros { get; init; }
    public ShoppingListDto? ShoppingList { get; init; }
}

public record DayPlanDto
{
    public int DayNumber { get; init; }
    public required string DayName { get; init; }
    public required List<MealPlanDto> Meals { get; init; }
    public required DayTotalsDto DayTotals { get; init; }
}

public record MealPlanDto
{
    public Guid PlanMealId { get; init; }
    public required string MealType { get; init; }
    public bool IsLocked { get; init; }
    public decimal PortionMultiplier { get; init; } = 1.0m;
    public long RowVersion { get; init; } = 1;
    public required RecipeDto Recipe { get; init; }
}

public record RecipeDto
{
    public Guid RecipeId { get; init; }
    public required string Name { get; init; }
    public int Calories { get; init; }
    public decimal ProteinGr { get; init; }
    public decimal FatGr { get; init; }
    public decimal CarbsGr { get; init; }
    public int PrepTimeMin { get; init; }
    public int CookTimeMin { get; init; }
    public required string Instructions { get; init; }
    public decimal EstimatedCostMxn { get; init; }
    public string? PrimaryStore { get; init; }
}

public record DayTotalsDto
{
    public int Calories { get; init; }
    public decimal ProteinGr { get; init; }
    public decimal FatGr { get; init; }
    public decimal CarbsGr { get; init; }
    public decimal EstimatedCostMxn { get; init; }
}

public record ShoppingListDto
{
    public Guid ShoppingListId { get; init; }
    public decimal TotalEstimatedMxn { get; init; }
    public required List<StoreGroupDto> ByStore { get; init; }
}

public record StoreGroupDto
{
    public required string StoreCode { get; init; }
    public required string StoreName { get; init; }
    public required List<ShoppingItemDto> Items { get; init; }
    public decimal SubtotalMxn { get; init; }
}

public record ShoppingItemDto
{
    public required string IngredientName { get; init; }
    public decimal TotalAmount { get; init; }
    public required string Unit { get; init; }
    public decimal EstimatedCostMxn { get; init; }
}

public class GeneratePlanCommandHandler : IRequestHandler<GeneratePlanCommand, Result<PlanGenerationResult>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IGeminiService _geminiService;
    private readonly IPlanValidator _planValidator;
    private readonly ICostEstimationService _costEstimationService;

    public GeneratePlanCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IGeminiService geminiService,
        IPlanValidator planValidator,
        ICostEstimationService costEstimationService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _geminiService = geminiService;
        _planValidator = planValidator;
        _costEstimationService = costEstimationService;
    }

    public async Task<Result<PlanGenerationResult>> Handle(GeneratePlanCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
            return Result<PlanGenerationResult>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var user = await _context.Users
            .Include(u => u.KetoProfile)
            .Include(u => u.MedicalProfile)
            .Include(u => u.UserGoals.Where(g => g.IsActive))
            .Include(u => u.BudgetMode)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return Result<PlanGenerationResult>.Failure("Usuario no encontrado.", "NOT_FOUND");

        if (user.EmailVerifiedAt is null)
            return Result<PlanGenerationResult>.Failure(
                "Debes verificar tu email antes de generar un plan.", "EMAIL_NOT_VERIFIED");

        if (user.DisclaimerAcceptedAt is null)
            return Result<PlanGenerationResult>.Failure(
                "Debes completar el onboarding antes de generar un plan.", "ONBOARDING_INCOMPLETE");

        if (user.MedicalProfile?.RequiresHumanReview == true && user.MedicalProfile?.OverrideAcceptedAt is null)
            return Result<PlanGenerationResult>.Failure(
                "Tu perfil médico requiere validación adicional.", "MEDICAL_OVERRIDE_REQUIRED");

        if (user.BudgetMode is null)
            return Result<PlanGenerationResult>.Failure(
                "Debes seleccionar un modo de presupuesto.", "BUDGET_MODE_REQUIRED");

        if (user.KetoProfile is null)
            return Result<PlanGenerationResult>.Failure(
                "Debes completar el onboarding para calcular tu perfil keto.", "KETO_PROFILE_MISSING");

        var activeGoal = user.UserGoals.FirstOrDefault(g => g.IsActive);

        // Verificar regeneraciones disponibles según plan de suscripción
        var userSubscription = await _context.UserSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId && s.Status == Domain.Enums.SubscriptionStatus.Active)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        int maxRegenerations = userSubscription?.Plan?.MaxRegenerationsWeek ?? 3;

        var weekStart = request.WeekStartDate;
        var weekEnd = weekStart.AddDays(7);
        var regenerationCount = await _context.WeeklyPlans
            .CountAsync(p => p.UserId == userId
                          && p.CreatedAt >= weekStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                          && p.CreatedAt < weekEnd.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), cancellationToken);

        if (regenerationCount >= maxRegenerations && request.ForceRegenerate)
            return Result<PlanGenerationResult>.Failure(
                $"Has alcanzado el límite de {maxRegenerations} regeneraciones esta semana.", "MAX_REGENERATIONS");

        var existingPlan = await _context.WeeklyPlans
            .Include(p => p.Meals)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive && p.StartDate == request.WeekStartDate, cancellationToken);

        if (existingPlan is not null && !request.ForceRegenerate)
            return Result<PlanGenerationResult>.Success(await MapToResult(existingPlan, cancellationToken));

        int age = DateTime.UtcNow.Year - user.BirthDate.Year;
        if (user.BirthDate > DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-age))) age--;

        var geminiRequest = new GeneratePlanRequest
        {
            UserId = userId,
            UserName = user.FullName,
            Age = age,
            Gender = user.Gender.ToString(),
            HeightCm = user.HeightCm,
            WeightKg = activeGoal?.StartWeightKg ?? 70m,
            TargetWeightKg = activeGoal?.TargetWeightKg,
            ActivityLevel = user.ActivityLevel.ToString(),
            BudgetModeCode = user.BudgetMode.Code,
            BudgetModeRulesJson = user.BudgetMode.Rules,
            DailyCalories = user.KetoProfile.DailyCalories,
            CarbsGrams = user.KetoProfile.CarbsGrams,
            ProteinGrams = user.KetoProfile.ProteinGrams,
            FatGrams = user.KetoProfile.FatGrams,
            Allergies = user.MedicalProfile?.Allergies ?? [],
            DislikedIngredients = user.MedicalProfile?.DislikedIngredients ?? [],
            DietaryRestrictions = user.MedicalProfile?.DietaryRestrictions ?? [],
            KetoExperienceLevel = (user.MedicalProfile?.KetoExperienceLevel ?? KetoExperienceLevel.Beginner).ToString(),
            IsOverridePlan = user.MedicalProfile?.OverrideAcceptedAt is not null,
            GoalType = (activeGoal?.GoalType ?? GoalType.WeightLoss).ToString(),
            WeekStartDate = request.WeekStartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            PreviousWeekRecipeCodes = [],
            FamilyContext = null,
        };

        GeneratePlanResponse geminiResponse;
        try
        {
            geminiResponse = await _geminiService.GeneratePlanAsync(geminiRequest, cancellationToken);
        }
        catch (Exception)
        {
            return Result<PlanGenerationResult>.Failure(
                "Error al generar el plan con IA. Intenta de nuevo.", "GEMINI_ERROR");
        }

        var validationContext = new PlanValidationContext
        {
            Allergies = user.MedicalProfile?.Allergies ?? [],
            DislikedIngredients = user.MedicalProfile?.DislikedIngredients ?? [],
            DietaryRestrictions = user.MedicalProfile?.DietaryRestrictions ?? [],
            DailyCaloriesTarget = user.KetoProfile.DailyCalories,
            ProteinTarget = user.KetoProfile.ProteinGrams,
            FatTarget = user.KetoProfile.FatGrams,
            CarbsTarget = user.KetoProfile.CarbsGrams,
            MaxCarbsGrams = user.MedicalProfile?.OverrideAcceptedAt is not null ? 60 : 50,
            BmrKcal = user.KetoProfile.BmrKcal ?? 0,
            TdeeKcal = user.KetoProfile.TdeeKcal ?? 0,
            IsOverridePlan = user.MedicalProfile?.OverrideAcceptedAt is not null,
            BudgetModeCode = user.BudgetMode.Code,
            WeightKg = activeGoal?.StartWeightKg ?? 70m,
            MinProteinPerKg = 0.8m,
        };

        var validationResult = _planValidator.Validate(geminiResponse, validationContext);
        if (!validationResult.IsValid)
            return Result<PlanGenerationResult>.Failure(
                $"El plan generado no pasó validación: {validationResult.ErrorMessage}", "PLAN_VALIDATION_FAILED");

        var costEstimate = await _costEstimationService.EstimatePlanCostAsync(
            geminiResponse, user.BudgetMode.Code, numberOfPeople: 1, cancellationToken);

        var activePlans = await _context.WeeklyPlans
            .Where(p => p.UserId == userId && p.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var plan in activePlans)
            plan.IsActive = false;

        var endDate = request.WeekStartDate.AddDays(6);
        var weeklyPlan = new WeeklyPlan
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StartDate = request.WeekStartDate,
            EndDate = endDate,
            IsOverridePlan = user.MedicalProfile?.OverrideAcceptedAt is not null,
            OriginalMenuContent = geminiResponse.RawJson,
            IsActive = true,
            GenerationSource = GenerationSource.Ai,
            BudgetModeId = user.BudgetModeId,
            EstimatedTotalCostMxn = costEstimate.TotalCostMxn,
            EstimatedCostPerPersonMxn = costEstimate.CostPerPersonMxn,
            EstimatedCostGourmetBaselineMxn = costEstimate.GourmetBaselineCostMxn,
            SavingsVsGourmetMxn = costEstimate.SavingsVsGourmetMxn,
            SavingsVsGourmetPercent = costEstimate.SavingsVsGourmetPercent,
        };
        _context.WeeklyPlans.Add(weeklyPlan);

        var mealTypes = new[] { "breakfast", "lunch", "dinner", "snack" };
        foreach (var day in geminiResponse.Days)
        {
            for (int i = 0; i < day.Meals.Count; i++)
            {
                var meal = day.Meals[i];
                var recipe = new Recipe
                {
                    Id = Guid.NewGuid(),
                    Name = meal.RecipeName,
                    MealType = Enum.Parse<MealType>(meal.MealType, ignoreCase: true),
                    Ingredients = JsonSerializer.Serialize(meal.Ingredients),
                    Instructions = meal.Instructions,
                    PrepTimeMin = meal.PrepTimeMin > 0 ? meal.PrepTimeMin : null,
                    CookTimeMin = meal.CookTimeMin > 0 ? meal.CookTimeMin : null,
                    Servings = meal.Servings,
                    BaseCalories = meal.TotalCalories,
                    BaseProteinGr = meal.TotalProteinG,
                    BaseFatGr = meal.TotalFatG,
                    BaseCarbsGr = meal.TotalCarbsG,
                    Source = RecipeSource.AiGenerated,
                    IsPublic = false,
                    CompatibleModeCodes = [user.BudgetMode.Code],
                    EstimatedCostPerServingMxn = meal.EstimatedCostMxn,
                };
                _context.Recipes.Add(recipe);

                var planMeal = new WeeklyPlanMeal
                {
                    Id = Guid.NewGuid(),
                    PlanId = weeklyPlan.Id,
                    DayOfWeek = day.DayNumber,
                    MealType = Enum.Parse<MealType>(meal.MealType, ignoreCase: true),
                    RecipeId = recipe.Id,
                    SortOrder = i + 1,
                };
                _context.WeeklyPlanMeals.Add(planMeal);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<PlanGenerationResult>.Success(await MapToResult(weeklyPlan, cancellationToken));
    }

    private async Task<PlanGenerationResult> MapToResult(WeeklyPlan plan, CancellationToken ct)
    {
        var meals = await _context.WeeklyPlanMeals
            .Include(m => m.Recipe)
            .Where(m => m.PlanId == plan.Id)
            .OrderBy(m => m.DayOfWeek).ThenBy(m => m.SortOrder)
            .ToListAsync(ct);

        var days = meals
            .GroupBy(m => m.DayOfWeek)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var dayMeals = g.OrderBy(m => m.SortOrder).ToList();
                return new DayPlanDto
                {
                    DayNumber = g.Key,
                    DayName = GetDayName(g.Key),
                    Meals = dayMeals.Select(m => new MealPlanDto
                    {
                        PlanMealId = m.Id,
                        MealType = m.MealType.ToString().ToLowerInvariant(),
                        IsLocked = m.IsLocked,
                        PortionMultiplier = m.PortionMultiplier,
                        RowVersion = m.RowVersion,
                        Recipe = new RecipeDto
                        {
                            RecipeId = m.Recipe!.Id,
                            Name = m.Recipe.Name,
                            Calories = m.Recipe.BaseCalories,
                            ProteinGr = m.Recipe.BaseProteinGr,
                            FatGr = m.Recipe.BaseFatGr,
                            CarbsGr = m.Recipe.BaseCarbsGr,
                            PrepTimeMin = m.Recipe.PrepTimeMin ?? 0,
                            CookTimeMin = m.Recipe.CookTimeMin ?? 0,
                            Instructions = m.Recipe.Instructions ?? "",
                            EstimatedCostMxn = m.Recipe.EstimatedCostPerServingMxn ?? 0,
                            PrimaryStore = null,
                        },
                    }).ToList(),
                    DayTotals = new DayTotalsDto
                    {
                        Calories = dayMeals.Sum(m => m.Recipe?.BaseCalories ?? 0),
                        ProteinGr = dayMeals.Sum(m => m.Recipe?.BaseProteinGr ?? 0),
                        FatGr = dayMeals.Sum(m => m.Recipe?.BaseFatGr ?? 0),
                        CarbsGr = dayMeals.Sum(m => m.Recipe?.BaseCarbsGr ?? 0),
                        EstimatedCostMxn = dayMeals.Sum(m => m.Recipe?.EstimatedCostPerServingMxn ?? 0),
                    },
                };
            }).ToList();

        var budgetMode = plan.BudgetModeId.HasValue
            ? await _context.BudgetModes.FindAsync([plan.BudgetModeId], ct)
            : null;

        return new PlanGenerationResult
        {
            PlanId = plan.Id,
            StartDate = plan.StartDate,
            EndDate = plan.EndDate,
            BudgetModeCode = budgetMode?.Code ?? "unknown",
            BudgetModeName = budgetMode?.Name ?? "Desconocido",
            IsOverridePlan = plan.IsOverridePlan,
            EstimatedCostMxn = plan.EstimatedTotalCostMxn,
            SavingsVsGourmetMxn = plan.SavingsVsGourmetMxn,
            SavingsVsGourmetPercent = plan.SavingsVsGourmetPercent,
            Days = days,
            Macros = new KetoProfileResult(),
            ShoppingList = null,
        };
    }

    private static string GetDayName(int day) => day switch
    {
        1 => "Lunes", 2 => "Martes", 3 => "Miércoles", 4 => "Jueves",
        5 => "Viernes", 6 => "Sábado", 7 => "Domingo", _ => "Día"
    };
}
