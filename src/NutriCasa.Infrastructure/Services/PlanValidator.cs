using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Infrastructure.Services;

public class PlanValidator : IPlanValidator
{
    public PlanValidationResult Validate(GeneratePlanResponse plan, PlanValidationContext context)
    {
        var warnings = new List<string>();

        if (plan.Days.Count != 7)
            return Fail($"El plan tiene {plan.Days.Count} días, deben ser 7.");

        for (int i = 0; i < plan.Days.Count; i++)
        {
            var day = plan.Days[i];
            if (day.Meals.Count != 4)
                return Fail($"El día {day.DayNumber} ({day.DayName}) tiene {day.Meals.Count} comidas, deben ser 4.");

            var mealTypes = day.Meals.Select(m => m.MealType.ToLowerInvariant()).ToList();
            if (!mealTypes.Contains("breakfast"))
                return Fail($"El día {day.DayNumber} no tiene desayuno.");
            if (!mealTypes.Contains("lunch"))
                return Fail($"El día {day.DayNumber} no tiene comida.");
            if (!mealTypes.Contains("dinner"))
                return Fail($"El día {day.DayNumber} no tiene cena.");
            if (!mealTypes.Contains("snack"))
                return Fail($"El día {day.DayNumber} no tiene snack.");

            if (day.DayTotals.CarbsG > context.MaxCarbsGrams)
                return Fail($"El día {day.DayNumber} tiene {day.DayTotals.CarbsG:F1}g de carbs, el límite es {context.MaxCarbsGrams}g. " +
                            $"Reduce los carbohidratos de las comidas de ese día.");

            decimal bmrFloor = context.BmrKcal * 0.85m;
            decimal tdeeCeiling = context.TdeeKcal * 1.10m;
            if (day.DayTotals.Calories < bmrFloor || day.DayTotals.Calories > tdeeCeiling)
                warnings.Add($"El día {day.DayNumber} tiene {day.DayTotals.Calories} kcal (rango: {bmrFloor:F0}-{tdeeCeiling:F0}).");

            if (day.DayTotals.ProteinG < context.MinProteinPerKg * context.WeightKg)
                return Fail($"El día {day.DayNumber} tiene solo {day.DayTotals.ProteinG:F1}g de proteína, " +
                            $"necesita al menos {context.MinProteinPerKg * context.WeightKg:F1}g.");

            foreach (var meal in day.Meals)
            {
                if (string.IsNullOrWhiteSpace(meal.RecipeName))
                    return Fail($"Una comida del día {day.DayNumber} no tiene nombre de receta.");

                if (string.IsNullOrWhiteSpace(meal.Instructions))
                    return Fail($"La receta '{meal.RecipeName}' no tiene instrucciones.");

                if (meal.Ingredients.Count == 0)
                    return Fail($"La receta '{meal.RecipeName}' no tiene ingredientes.");

                foreach (var ingredient in meal.Ingredients)
                {
                    string ingName = ingredient.Name.ToLowerInvariant();
                    foreach (var allergy in context.Allergies)
                    {
                        if (ingName.Contains(allergy.ToLowerInvariant()))
                            return Fail($"La receta '{meal.RecipeName}' contiene '{ingredient.Name}' que está en tus alergias.");
                    }

                    foreach (var disliked in context.DislikedIngredients)
                    {
                        if (ingName.Contains(disliked.ToLowerInvariant()))
                            return Fail($"La receta '{meal.RecipeName}' contiene '{ingredient.Name}' que no te gusta.");
                    }
                }

                foreach (var restriction in context.DietaryRestrictions)
                {
                    string r = restriction.ToLowerInvariant();
                    bool hasIssue = meal.Ingredients.Any(i =>
                    {
                        string iname = i.Name.ToLowerInvariant();
                        return (r == "no_pork" && (iname.Contains("cerdo") || iname.Contains("puerco") || iname.Contains("lomo")))
                            || (r == "vegetarian" && (iname.Contains("pollo") || iname.Contains("carne") || iname.Contains("pescado") || iname.Contains("res") || iname.Contains("cerdo")))
                            || (r == "vegan" && (iname.Contains("huevo") || iname.Contains("leche") || iname.Contains("queso") || iname.Contains("crema") || iname.Contains("mantequilla") || iname.Contains("yogur")));
                    });
                    if (hasIssue)
                        return Fail($"La receta '{meal.RecipeName}' viola la restricción dietética '{restriction}'.");
                }

                if (meal.EstimatedCostMxn < 0)
                    warnings.Add($"La receta '{meal.RecipeName}' tiene un costo negativo.");
            }
        }

        var recipeNames = plan.Days.SelectMany(d => d.Meals).Select(m => m.RecipeName.ToLowerInvariant()).ToList();
        var duplicates = recipeNames.GroupBy(n => n).Where(g => g.Count() > 3).ToList();
        if (duplicates.Count > 0)
        {
            foreach (var dup in duplicates)
                warnings.Add($"La receta '{dup.Key}' aparece {dup.Count()} veces (máximo 3).");
        }

        if (plan.PlanMetadata.EstimatedTotalCostMxn < 0)
            warnings.Add("El costo total estimado es negativo.");

        return new PlanValidationResult
        {
            IsValid = true,
            Warnings = warnings,
        };
    }

    private static PlanValidationResult Fail(string message)
    {
        return new PlanValidationResult
        {
            IsValid = false,
            ErrorMessage = message,
            CorrectionPrompt = $"Corrige: {message}",
        };
    }
}
