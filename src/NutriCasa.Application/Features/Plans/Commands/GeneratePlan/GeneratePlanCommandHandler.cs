using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Services;
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
    public List<RecipeIngredientDto> Ingredients { get; init; } = [];
}

public record RecipeIngredientDto
{
    public required string Name { get; init; }
    public decimal Amount { get; init; }
    public string Unit { get; init; } = "";
}

public static class RecipeIngredientParser
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    // Tolera ambos formatos almacenados en Recipe.Ingredients:
    //  - Curado: { code, name, amount, unit }
    //  - IA:     { IngredientCode, Name, AmountGr, UnitLabel }
    public static List<RecipeIngredientDto> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return [];

            var result = new List<RecipeIngredientDto>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;

                var name = GetString(item, "name") ?? GetString(item, "Name");
                if (string.IsNullOrWhiteSpace(name)) continue;

                var amount = GetDecimal(item, "amount") ?? GetDecimal(item, "amountGr")
                    ?? GetDecimal(item, "amount_gr") ?? GetDecimal(item, "AmountGr") ?? 0m;
                var unit = GetString(item, "unit") ?? GetString(item, "unitLabel")
                    ?? GetString(item, "unit_label") ?? GetString(item, "UnitLabel") ?? "";

                result.Add(new RecipeIngredientDto { Name = name, Amount = amount, Unit = unit });
            }
            return result;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? GetString(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static decimal? GetDecimal(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDecimal() : null;
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
    private readonly IFeatureFlagService _featureFlags;

    public GeneratePlanCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IGeminiService geminiService,
        IPlanValidator planValidator,
        ICostEstimationService costEstimationService,
        IFeatureFlagService featureFlags)
    {
        _context = context;
        _currentUserService = currentUserService;
        _geminiService = geminiService;
        _planValidator = planValidator;
        _costEstimationService = costEstimationService;
        _featureFlags = featureFlags;
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

        int age = DateTime.UtcNow.Year - user.BirthDate.Year;
        if (user.BirthDate > DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-age))) age--;

        if (age < 18)
            return Result<PlanGenerationResult>.Failure(
                "Necesitas tener al menos 18 años para generar un plan keto en NutriCasa.", "MINOR_NOT_ALLOWED");

        if (user.EmailVerifiedAt is null)
            return Result<PlanGenerationResult>.Failure(
                "Debes verificar tu email antes de generar un plan.", "EMAIL_NOT_VERIFIED");

        if (user.DisclaimerAcceptedAt is null)
            return Result<PlanGenerationResult>.Failure(
                "Debes completar el onboarding antes de generar un plan.", "ONBOARDING_INCOMPLETE");

        if (user.MedicalProfile is not null)
        {
            user.MedicalProfile.RequiresHumanReview = MedicalSafetyRules.RequiresHumanReview(user.MedicalProfile);

            if (MedicalSafetyRules.HasAbsoluteKetoBlock(user.MedicalProfile, age))
                return Result<PlanGenerationResult>.Failure(
                    MedicalSafetyRules.GetAbsoluteBlockMessage(user.MedicalProfile, age), "MEDICAL_ABSOLUTE_BLOCK");

            if (user.MedicalProfile.RequiresHumanReview && user.MedicalProfile.OverrideAcceptedAt is null)
                return Result<PlanGenerationResult>.Failure(
                    "Tu perfil médico requiere validación adicional.", "MEDICAL_OVERRIDE_REQUIRED");
        }

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

        // Fase de arranque: regeneración ilimitada (ver IFeatureFlagService).
        if (!_featureFlags.UnlimitedRegeneration)
        {
            var weekStart = request.WeekStartDate;
            var weekEnd = weekStart.AddDays(7);
            var regenerationCount = await _context.WeeklyPlans
                .CountAsync(p => p.UserId == userId
                              && p.CreatedAt >= weekStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                              && p.CreatedAt < weekEnd.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), cancellationToken);

            if (regenerationCount >= maxRegenerations && request.ForceRegenerate)
                return Result<PlanGenerationResult>.Failure(
                    $"Has alcanzado el límite de {maxRegenerations} regeneraciones esta semana.", "MAX_REGENERATIONS");
        }

        var existingPlan = await _context.WeeklyPlans
            .Include(p => p.Meals)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive && p.StartDate == request.WeekStartDate, cancellationToken);

        if (existingPlan is not null && !request.ForceRegenerate)
            return Result<PlanGenerationResult>.Success(await MapToResult(existingPlan, cancellationToken));

        // ─── Contexto familiar: grupo, hogar, miembros y recetas existentes ───
        var membership = await _context.GroupMemberships
            .Include(m => m.Group)
            .FirstOrDefaultAsync(m => m.UserId == userId && m.LeftAt == null, cancellationToken);

        Guid? groupId = membership?.GroupId;
        string? familyContext = null;

        if (membership is not null)
        {
            // Obtener los miembros del mismo grupo
            var householdMembers = await _context.GroupMemberships
                .Include(m => m.User)
                .Where(m => m.GroupId == membership.GroupId && m.LeftAt == null && m.UserId != userId)
                .Select(m => m.User.FullName)
                .ToListAsync(cancellationToken);

            // Buscar recetas ya asignadas a otros miembros del grupo para esta semana
            var householdMemberIds = await _context.GroupMemberships
                .Where(m => m.GroupId == membership.GroupId && m.LeftAt == null && m.UserId != userId)
                .Select(m => m.UserId)
                .ToListAsync(cancellationToken);

            var otherMembersRecipes = new List<string>();
            if (householdMemberIds.Count > 0)
            {
                otherMembersRecipes = await _context.WeeklyPlans
                    .Where(p => householdMemberIds.Contains(p.UserId)
                             && p.IsActive
                             && p.StartDate == request.WeekStartDate)
                    .SelectMany(p => p.Meals)
                    .Where(m => m.Recipe != null)
                    .Select(m => m.Recipe!.Name)
                    .Distinct()
                    .ToListAsync(cancellationToken);
            }

            // Construir el contexto para Gemini
            var parts = new List<string>();
            var totalHousehold = householdMembers.Count + 1;
            parts.Add($"Cocina para un hogar de {totalHousehold} persona{(totalHousehold > 1 ? "s" : "")}.");

            if (householdMembers.Count > 0)
                parts.Add($"Otros miembros del hogar: {string.Join(", ", householdMembers)}.");

            if (otherMembersRecipes.Count > 0)
            {
                var recipeSample = otherMembersRecipes.Take(15);
                parts.Add($"Platillos ya asignados a otros miembros esta semana (prioriza ingredientes en común para optimizar despensa): {string.Join(", ", recipeSample)}.");
            }

            parts.Add("IMPORTANTE: Prioriza recetas que compartan ingredientes base con los otros miembros del hogar para reducir costo de despensa.");

            familyContext = string.Join(" ", parts);
        }

        // ─── Recetas de la semana pasada para evitar repetición excesiva ───
        var previousWeekStart = request.WeekStartDate.AddDays(-7);
        var previousWeekRecipes = await _context.WeeklyPlans
            .Where(p => p.UserId == userId && p.StartDate == previousWeekStart)
            .SelectMany(p => p.Meals)
            .Where(m => m.Recipe != null)
            .Select(m => m.Recipe!.Name)
            .Distinct()
            .ToListAsync(cancellationToken);

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
            PreviousWeekRecipeCodes = previousWeekRecipes.ToArray(),
            FamilyContext = familyContext,
        };

        GeneratePlanResponse geminiResponse;
        try
        {
            geminiResponse = await _geminiService.GeneratePlanAsync(geminiRequest, cancellationToken);
        }
        catch (Exception)
        {
            return await GenerateCuratedFallbackPlanAsync(user, activeGoal, request, userId, cancellationToken);
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
            return await GenerateCuratedFallbackPlanAsync(user, activeGoal, request, userId, cancellationToken);

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
            GroupId = groupId,
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

        var ketoProfile = await _context.Users
            .Where(u => u.Id == plan.UserId)
            .Select(u => u.KetoProfile)
            .FirstOrDefaultAsync(ct);

        var catalogRaw = await _context.IngredientCatalog
            .Where(c => c.IsActive && c.PrimaryStoreCategory != null)
            .Select(c => new { c.Name, c.PrimaryStoreCategory })
            .ToListAsync(ct);
        var catalog = catalogRaw
            .Select(c => (c.Name.ToLowerInvariant(), c.PrimaryStoreCategory!))
            .ToList();

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
                            Ingredients = RecipeIngredientParser.Parse(m.Recipe.Ingredients),
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
            Macros = ketoProfile is not null ? new KetoProfileResult
            {
                BmrKcal = ketoProfile.BmrKcal ?? 0,
                TdeeKcal = ketoProfile.TdeeKcal ?? 0,
                DailyCalories = ketoProfile.DailyCalories,
                CarbsGrams = ketoProfile.CarbsGrams,
                ProteinGrams = ketoProfile.ProteinGrams,
                FatGrams = ketoProfile.FatGrams,
                CarbsPercent = ketoProfile.CarbsPercent ?? 0,
                ProteinPercent = ketoProfile.ProteinPercent ?? 0,
                FatPercent = ketoProfile.FatPercent ?? 0,
            } : new KetoProfileResult(),
            ShoppingList = BuildShoppingList(meals, catalog),
        };
    }

    internal static ShoppingListDto BuildShoppingList(
        IEnumerable<WeeklyPlanMeal> meals,
        IReadOnlyList<(string NameLower, string StoreCode)>? catalog = null)
    {
        var consolidated = new Dictionary<string, (decimal Amount, string Unit)>(StringComparer.OrdinalIgnoreCase);

        foreach (var meal in meals.Where(m => m.Recipe is not null))
        {
            foreach (var ing in RecipeIngredientParser.Parse(meal.Recipe!.Ingredients))
            {
                if (consolidated.TryGetValue(ing.Name, out var existing))
                    consolidated[ing.Name] = (existing.Amount + ing.Amount, ing.Unit);
                else
                    consolidated[ing.Name] = (ing.Amount, ing.Unit);
            }
        }

        // Agrupar por tienda usando el catálogo de ingredientes
        var byStore = new Dictionary<string, List<ShoppingItemDto>>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in consolidated.OrderBy(k => k.Key))
        {
            var storeCode = catalog is not null ? ResolveStore(kv.Key, catalog) : "supermercado";
            if (!byStore.TryGetValue(storeCode, out var bucket))
            {
                bucket = [];
                byStore[storeCode] = bucket;
            }
            bucket.Add(new ShoppingItemDto
            {
                IngredientName = kv.Key,
                TotalAmount = Math.Round(kv.Value.Amount, 1),
                Unit = kv.Value.Unit,
                EstimatedCostMxn = 0m,
            });
        }

        var storeOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["mercado_tradicional"] = 0, ["supermercado"] = 1,
            ["pescaderia"] = 2, ["tienda_especializada"] = 3,
        };
        var storeDisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["mercado_tradicional"] = "Mercado", ["supermercado"] = "Supermercado",
            ["pescaderia"] = "Pescadería", ["tienda_especializada"] = "Tienda especializada",
        };

        var groups = byStore
            .OrderBy(kv => storeOrder.TryGetValue(kv.Key, out var o) ? o : 99)
            .Select(kv => new StoreGroupDto
            {
                StoreCode = kv.Key,
                StoreName = storeDisplayNames.TryGetValue(kv.Key, out var name) ? name : kv.Key,
                Items = kv.Value,
                SubtotalMxn = 0m,
            }).ToList();

        if (groups.Count == 0)
            groups = [new StoreGroupDto
            {
                StoreCode = "general", StoreName = "Lista de compras",
                Items = [], SubtotalMxn = 0m,
            }];

        return new ShoppingListDto
        {
            ShoppingListId = Guid.Empty,
            TotalEstimatedMxn = 0m,
            ByStore = groups,
        };
    }

    private static string ResolveStore(
        string ingredientName,
        IReadOnlyList<(string NameLower, string StoreCode)> catalog)
    {
        var lower = ingredientName.ToLowerInvariant();
        string? best = null;
        int bestLen = 0;
        foreach (var (catalogName, storeCode) in catalog)
        {
            if (lower.Contains(catalogName) && catalogName.Length > bestLen)
            {
                best = storeCode;
                bestLen = catalogName.Length;
            }
        }
        return best ?? "supermercado";
    }

    private async Task<Result<PlanGenerationResult>> GenerateCuratedFallbackPlanAsync(
        User user,
        UserGoal? activeGoal,
        GeneratePlanCommand request,
        Guid userId,
        CancellationToken ct)
    {
        // El respaldo se ejecuta precisamente cuando Gemini falló o tardó demasiado.
        // Si la petición ya fue cancelada (p.ej. el cliente agotó su timeout durante la
        // llamada lenta a Gemini), NO debemos heredar ese token: queremos persistir el
        // plan de respaldo de todos modos para que el usuario lo vea al reintentar.
        ct = CancellationToken.None;

        var budgetModeCode = user.BudgetMode?.Code ?? "pantry_basic";

        var curatedRecipes = await _context.Recipes
            .Where(r => r.Source == RecipeSource.Curated
                     && r.NutritionTrack == NutritionTrack.Keto
                     && (r.CompatibleModeCodes.Contains(budgetModeCode) || r.CompatibleModeCodes.Length == 0))
            .OrderBy(r => r.MealType)
            .ThenBy(r => r.UseCount)
            .ThenBy(r => r.Name)
            .ToListAsync(ct);

        if (curatedRecipes.Count < 4)
        {
            curatedRecipes = await _context.Recipes
                .Where(r => r.Source == RecipeSource.Curated && r.NutritionTrack == NutritionTrack.Keto)
                .OrderBy(r => r.MealType)
                .ThenBy(r => r.UseCount)
                .ThenBy(r => r.Name)
                .ToListAsync(ct);
        }

        if (curatedRecipes.Count == 0)
            return Result<PlanGenerationResult>.Failure(
                "No pudimos generar el plan y no hay recetas curadas disponibles como respaldo.", "NO_CURATED_FALLBACK");

        var activePlans = await _context.WeeklyPlans
            .Where(p => p.UserId == userId && p.IsActive)
            .ToListAsync(ct);

        foreach (var plan in activePlans)
            plan.IsActive = false;

        var weeklyPlan = new WeeklyPlan
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StartDate = request.WeekStartDate,
            EndDate = request.WeekStartDate.AddDays(6),
            IsOverridePlan = user.MedicalProfile?.OverrideAcceptedAt is not null,
            OriginalMenuContent = JsonSerializer.Serialize(new
            {
                source = "curated_fallback",
                reason = "gemini_failed_or_validation_failed",
                budget_mode = budgetModeCode,
                generated_at = DateTime.UtcNow,
            }),
            IsActive = true,
            GenerationSource = GenerationSource.Fallback,
            BudgetModeId = user.BudgetModeId,
        };

        var totalCost = 0m;
        var mealTypes = new[] { MealType.Breakfast, MealType.Lunch, MealType.Dinner, MealType.Snack };

        // Cada tipo tiene su propio pool barajado. Si un tipo no tiene recetas propias,
        // usa el catálogo completo como respaldo.
        var rng = new Random(userId.GetHashCode() ^ request.WeekStartDate.GetHashCode());
        var poolByType = mealTypes.ToDictionary(
            type => type,
            type =>
            {
                var matching = curatedRecipes.Where(r => r.MealType == type).ToList();
                var pool = matching.Count > 0 ? matching : curatedRecipes;
                return pool.OrderBy(_ => rng.Next()).ToList();
            });
        var indexByType = mealTypes.ToDictionary(type => type, _ => 0);

        for (int day = 1; day <= 7; day++)
        {
            var usedToday = new HashSet<Guid>();
            for (int i = 0; i < mealTypes.Length; i++)
            {
                var type = mealTypes[i];
                var pool = poolByType[type];

                // Avanza el índice rotatorio evitando repetir una receta ya usada ese día.
                Recipe recipe;
                int guard = 0;
                do
                {
                    recipe = pool[indexByType[type] % pool.Count];
                    indexByType[type]++;
                    guard++;
                } while (usedToday.Contains(recipe.Id) && guard <= pool.Count);

                usedToday.Add(recipe.Id);
                recipe.UseCount++;
                totalCost += recipe.EstimatedCostPerServingMxn ?? 0m;

                _context.WeeklyPlanMeals.Add(new WeeklyPlanMeal
                {
                    Id = Guid.NewGuid(),
                    PlanId = weeklyPlan.Id,
                    DayOfWeek = day,
                    MealType = type,
                    RecipeId = recipe.Id,
                    SortOrder = i + 1,
                });
            }
        }

        weeklyPlan.EstimatedTotalCostMxn = totalCost;
        weeklyPlan.EstimatedCostPerPersonMxn = totalCost;
        weeklyPlan.EstimatedCostGourmetBaselineMxn = totalCost;
        weeklyPlan.SavingsVsGourmetMxn = 0m;
        weeklyPlan.SavingsVsGourmetPercent = 0m;

        _context.WeeklyPlans.Add(weeklyPlan);
        await _context.SaveChangesAsync(ct);

        return Result<PlanGenerationResult>.Success(await MapToResult(weeklyPlan, ct));
    }

    private static string GetDayName(int day) => day switch
    {
        1 => "Lunes", 2 => "Martes", 3 => "Miércoles", 4 => "Jueves",
        5 => "Viernes", 6 => "Sábado", 7 => "Domingo", _ => "Día"
    };
}
