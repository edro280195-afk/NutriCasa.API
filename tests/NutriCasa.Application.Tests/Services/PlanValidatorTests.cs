using NutriCasa.Application.Common.Models;
using NutriCasa.Infrastructure.Services;
using Xunit;

namespace NutriCasa.Application.Tests.Services;

public class PlanValidatorTests
{
    private readonly PlanValidator _sut = new();

    [Fact]
    public void Validate_PlanConCarbs65g_Falla()
    {
        var plan = CreateValidPlan();
        // Modificar carbs del día 1 para exceder el límite
        plan.Days[0] = plan.Days[0] with
        {
            DayTotals = plan.Days[0].DayTotals with { CarbsG = 65m }
        };

        var context = CreateContext(maxCarbsGrams: 50);

        var result = _sut.Validate(plan, context);

        Assert.False(result.IsValid);
        Assert.Contains("carbs", result.ErrorMessage?.ToLowerInvariant() ?? "");
    }

    [Fact]
    public void Validate_PlanConIngredienteAlergico_Falla()
    {
        var plan = CreateValidPlan();
        // Agregar ingrediente alergénico al primer meal
        var meals = plan.Days[0].Meals.ToList();
        meals[0] = meals[0] with
        {
            Ingredients = meals[0].Ingredients.Concat(new[]
            {
                new RecipeIngredient { Name = "Camarones al mojo de ajo", AmountGr = 100 }
            }).ToList()
        };
        plan.Days[0] = plan.Days[0] with { Meals = meals };

        var context = CreateContext(allergies: ["mariscos", "camarones"]);

        var result = _sut.Validate(plan, context);

        Assert.False(result.IsValid);
        Assert.Contains("alerg", result.ErrorMessage?.ToLowerInvariant() ?? "");
    }

    [Fact]
    public void Validate_PlanValido_Pasa()
    {
        var plan = CreateValidPlan();
        var context = CreateContext();

        var result = _sut.Validate(plan, context);

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Validate_PlanConMenosDe7Dias_Falla()
    {
        var plan = CreateValidPlan();
        plan = plan with { Days = plan.Days.Take(5).ToList() };

        var context = CreateContext();

        var result = _sut.Validate(plan, context);

        Assert.False(result.IsValid);
        Assert.Contains("5 días", result.ErrorMessage);
    }

    [Fact]
    public void Validate_PlanSinDesayuno_Falla()
    {
        var plan = CreateValidPlan();
        var meals = plan.Days[0].Meals.ToList();
        meals.RemoveAll(m => m.MealType == "breakfast");
        meals.Add(new MealPlan
        {
            MealType = "lunch",
            RecipeName = "Comida extra",
            Ingredients = new List<RecipeIngredient> { new() { Name = "Pollo" } },
            Instructions = "Cocinar"
        });
        plan.Days[0] = plan.Days[0] with { Meals = meals };

        var context = CreateContext();

        var result = _sut.Validate(plan, context);

        Assert.False(result.IsValid);
        Assert.Contains("desayuno", result.ErrorMessage?.ToLowerInvariant() ?? "");
    }

    [Fact]
    public void Validate_PlanConProteinaInsuficiente_Falla()
    {
        var plan = CreateValidPlan();
        plan.Days[0] = plan.Days[0] with
        {
            DayTotals = plan.Days[0].DayTotals with { ProteinG = 30m } // 0.8 * 90 = 72g mínimo
        };

        var context = CreateContext(weightKg: 90, minProteinPerKg: 0.8m);

        var result = _sut.Validate(plan, context);

        Assert.False(result.IsValid);
        Assert.Contains("prote", result.ErrorMessage?.ToLowerInvariant() ?? "");
    }

    // ─── Helpers ─────────────────────────────────────────────────────

    private static GeneratePlanResponse CreateValidPlan()
    {
        var days = new List<DayPlan>();
        for (int d = 1; d <= 7; d++)
        {
            var meals = new List<MealPlan>();
            foreach (var type in new[] { "breakfast", "lunch", "dinner", "snack" })
            {
                meals.Add(new MealPlan
                {
                    MealType = type,
                    RecipeName = $"Receta {type} día {d}",
                    Ingredients = new List<RecipeIngredient>
                    {
                        new() { Name = "Pollo", AmountGr = 150, Kcal = 247, ProteinG = 46, FatG = 5, CarbsG = 0 },
                        new() { Name = "Aguacate", AmountGr = 100, Kcal = 160, ProteinG = 2, FatG = 15, CarbsG = 2 }
                    },
                    Instructions = $"Preparar receta del {type}",
                    TotalCalories = 500, TotalProteinG = 35, TotalFatG = 40, TotalCarbsG = 5,
                    EstimatedCostMxn = 45.50m,
                });
            }

            days.Add(new DayPlan
            {
                DayNumber = d,
                DayName = $"Día {d}",
                Meals = meals,
                DayTotals = new DayTotals
                {
                    Calories = 2000, ProteinG = 140, FatG = 160, CarbsG = 20,
                    EstimatedCostMxn = 182m,
                }
            });
        }

        return new GeneratePlanResponse
        {
            PlanMetadata = new PlanMetadata
            {
                ModeCode = "pantry_basic",
                EstimatedTotalCostMxn = 1274m
            },
            MacrosTarget = new MacrosTarget
            {
                DailyCalories = 2000, DailyProteinG = 140, DailyFatG = 160, DailyCarbsG = 20
            },
            Days = days,
            ShoppingListConsolidated = new List<ShoppingListConsolidatedItem>()
        };
    }

    private static PlanValidationContext CreateContext(
        int maxCarbsGrams = 50,
        string[]? allergies = null,
        decimal weightKg = 90m,
        decimal minProteinPerKg = 0.8m)
    {
        return new PlanValidationContext
        {
            Allergies = allergies ?? [],
            DislikedIngredients = [],
            DietaryRestrictions = [],
            DailyCaloriesTarget = 2000,
            ProteinTarget = 140,
            FatTarget = 160,
            CarbsTarget = 20,
            MaxCarbsGrams = maxCarbsGrams,
            BmrKcal = 1824m,
            TdeeKcal = 2189m,
            IsOverridePlan = false,
            BudgetModeCode = "pantry_basic",
            WeightKg = weightKg,
            MinProteinPerKg = minProteinPerKg,
        };
    }
}
