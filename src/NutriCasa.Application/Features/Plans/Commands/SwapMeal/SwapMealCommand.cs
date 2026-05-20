using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.Plans.Commands.SwapMeal;

public record SwapMealCommand : IRequest<Result>
{
    public required Guid PlanMealId { get; init; }
    public string? SwapReason { get; init; }
}

public class SwapMealCommandHandler : IRequestHandler<SwapMealCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IGeminiService _geminiService;
    private readonly IPlanValidator _planValidator;
    private readonly IIngredientSubstitutionService _substitutionService;
    private readonly IFeatureFlagService _featureFlags;

    public SwapMealCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IGeminiService geminiService,
        IPlanValidator planValidator,
        IIngredientSubstitutionService substitutionService,
        IFeatureFlagService featureFlags)
    {
        _context = context;
        _currentUserService = currentUserService;
        _geminiService = geminiService;
        _planValidator = planValidator;
        _substitutionService = substitutionService;
        _featureFlags = featureFlags;
    }

    public async Task<Result> Handle(SwapMealCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var meal = await _context.WeeklyPlanMeals
            .Include(m => m.Plan).ThenInclude(p => p.User).ThenInclude(u => u.KetoProfile)
            .Include(m => m.Plan).ThenInclude(p => p.User).ThenInclude(u => u.MedicalProfile)
            .Include(m => m.Plan).ThenInclude(p => p.BudgetMode)
            .Include(m => m.Recipe)
            .FirstOrDefaultAsync(m => m.Id == request.PlanMealId, cancellationToken);

        if (meal is null)
            return Result.Failure("Comida no encontrada.", "NOT_FOUND");

        if (meal.Plan.UserId != userId)
            return Result.Failure("No tienes permiso para modificar este plan.", "FORBIDDEN");

        if (meal.IsLocked)
            return Result.Failure("La comida está bloqueada. Desbloquéala antes de cambiarla.", "MEAL_LOCKED");

        // Fase de arranque: sustituciones ilimitadas (ver IFeatureFlagService).
        if (!_featureFlags.UnlimitedRegeneration)
        {
            var swapCount = await _context.MealLogs
                .CountAsync(l => l.UserId == userId
                              && l.PlanMealId == request.PlanMealId
                              && l.Status == MealLogStatus.Substituted, cancellationToken);

            var maxSwaps = 10; // system_threshold
            if (swapCount >= maxSwaps)
                return Result.Failure("Has alcanzado el límite de sustituciones esta semana.", "MAX_SWAPS");
        }

        var user = meal.Plan.User;
        var budgetMode = meal.Plan.BudgetMode;

        var swapRequest = new SwapMealRequest
        {
            UserId = userId,
            PlanMealId = request.PlanMealId,
            CurrentRecipeName = meal.Recipe?.Name ?? "Receta",
            MealType = meal.MealType.ToString().ToLowerInvariant(),
            BudgetModeCode = budgetMode?.Code ?? "pantry_basic",
            Allergies = user.MedicalProfile?.Allergies ?? [],
            DislikedIngredients = user.MedicalProfile?.DislikedIngredients ?? [],
            DailyCaloriesTarget = user.KetoProfile?.DailyCalories ?? 600,
            ProteinTarget = user.KetoProfile?.ProteinGrams ?? 30,
            FatTarget = user.KetoProfile?.FatGrams ?? 40,
            CarbsTarget = user.KetoProfile?.CarbsGrams ?? 10,
            SwapReason = request.SwapReason,
        };

        SwapMealResponse? swapResponse = null;
        try
        {
            swapResponse = await _geminiService.SwapMealAsync(swapRequest, cancellationToken);
        }
        catch (Exception)
        {
            swapResponse = null;
        }

        var previousRecipeId = meal.RecipeId;
        Guid newRecipeId;
        string newRecipeName;

        if (swapResponse is not null)
        {
            // La IA generó una receta nueva.
            var newRecipe = new Recipe
            {
                Id = Guid.NewGuid(),
                Name = swapResponse.NewMeal.RecipeName,
                MealType = Enum.Parse<MealType>(swapResponse.NewMeal.MealType, ignoreCase: true),
                Ingredients = System.Text.Json.JsonSerializer.Serialize(swapResponse.NewMeal.Ingredients),
                Instructions = swapResponse.NewMeal.Instructions,
                PrepTimeMin = swapResponse.NewMeal.PrepTimeMin > 0 ? swapResponse.NewMeal.PrepTimeMin : null,
                CookTimeMin = swapResponse.NewMeal.CookTimeMin > 0 ? swapResponse.NewMeal.CookTimeMin : null,
                Servings = swapResponse.NewMeal.Servings,
                BaseCalories = swapResponse.NewMeal.TotalCalories,
                BaseProteinGr = swapResponse.NewMeal.TotalProteinG,
                BaseFatGr = swapResponse.NewMeal.TotalFatG,
                BaseCarbsGr = swapResponse.NewMeal.TotalCarbsG,
                Source = RecipeSource.AiGenerated,
                IsPublic = false,
                CompatibleModeCodes = [budgetMode?.Code ?? "pantry_basic"],
                EstimatedCostPerServingMxn = swapResponse.NewMeal.EstimatedCostMxn,
            };
            _context.Recipes.Add(newRecipe);
            newRecipeId = newRecipe.Id;
            newRecipeName = newRecipe.Name;
        }
        else
        {
            // Fallback: si la IA no está disponible, sustituimos por una receta
            // curada distinta del mismo tipo para que "generar otra" siempre funcione.
            var fallback = await PickCuratedAlternativeAsync(
                meal.MealType, previousRecipeId, budgetMode?.Code ?? "pantry_basic", cancellationToken);

            if (fallback is null)
                return Result.Failure("No hay recetas alternativas disponibles para esta comida.", "NO_ALTERNATIVE");

            fallback.UseCount++;
            newRecipeId = fallback.Id;
            newRecipeName = fallback.Name;
        }

        // Registrar el swap como MealLog
        var log = new MealLog
        {
            UserId = userId,
            PlanMealId = request.PlanMealId,
            RecipeId = previousRecipeId,
            Status = MealLogStatus.Substituted,
            SubstitutionNote = request.SwapReason ?? $"Sustituido por: {newRecipeName}",
            LoggedForDate = DateOnly.FromDateTime(DateTime.UtcNow),
            LoggedAt = DateTime.UtcNow,
        };
        _context.MealLogs.Add(log);

        // Asignar nueva receta al plan
        meal.RecipeId = newRecipeId;
        meal.RowVersion++;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task<Recipe?> PickCuratedAlternativeAsync(
        MealType mealType, Guid? currentRecipeId, string budgetModeCode, CancellationToken ct)
    {
        var candidates = await _context.Recipes
            .Where(r => r.Source == RecipeSource.Curated
                     && r.NutritionTrack == NutritionTrack.Keto
                     && r.MealType == mealType
                     && r.Id != currentRecipeId
                     && (r.CompatibleModeCodes.Contains(budgetModeCode) || r.CompatibleModeCodes.Length == 0))
            .OrderBy(r => r.UseCount)
            .Take(10)
            .ToListAsync(ct);

        if (candidates.Count == 0)
        {
            candidates = await _context.Recipes
                .Where(r => r.Source == RecipeSource.Curated
                         && r.MealType == mealType
                         && r.Id != currentRecipeId)
                .OrderBy(r => r.UseCount)
                .Take(10)
                .ToListAsync(ct);
        }

        if (candidates.Count == 0) return null;

        return candidates[Random.Shared.Next(candidates.Count)];
    }
}
