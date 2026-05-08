using NutriCasa.Application.Common.Models;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Services;

/// <summary>
/// Calcula BMR (Mifflin-St Jeor) + TDEE + distribución de macros keto.
/// Los umbrales de piso/techo se leen en tiempo de llamada desde system_thresholds.
/// </summary>
public class KetoProfileCalculator
{
    // ─── Multiplicadores TDEE ─────────────────────────────────────────────────
    private static readonly Dictionary<ActivityLevel, decimal> TdeeMultipliers = new()
    {
        [ActivityLevel.Sedentary]  = 1.2m,
        [ActivityLevel.Light]      = 1.375m,
        [ActivityLevel.Moderate]   = 1.55m,
        [ActivityLevel.Active]     = 1.725m,
        [ActivityLevel.VeryActive] = 1.9m,
    };

    // ─── Factores de calorías por objetivo ───────────────────────────────────
    private static readonly Dictionary<GoalType, decimal> CalorieFactors = new()
    {
        [GoalType.WeightLoss]  = 0.80m,
        [GoalType.BodyRecomp]  = 0.90m,
        [GoalType.Maintenance] = 1.00m,
        [GoalType.MuscleGain]  = 1.10m,
        [GoalType.Health]      = 0.90m,
    };

    /// <summary>
    /// Calcula el perfil keto completo para un usuario.
    /// </summary>
    /// <param name="weightKg">Peso actual en kg</param>
    /// <param name="heightCm">Altura en cm</param>
    /// <param name="age">Edad en años</param>
    /// <param name="gender">Género del usuario</param>
    /// <param name="activityLevel">Nivel de actividad física</param>
    /// <param name="goalType">Tipo de objetivo</param>
    /// <param name="bmrCalorieFloorFactor">Factor piso BMR desde system_thresholds (ej: 0.85)</param>
    /// <param name="tdeeCalorieCeilingFactor">Factor techo TDEE desde system_thresholds (ej: 1.10)</param>
    /// <param name="minimumProteinPerKg">Proteína mínima g/kg desde system_thresholds (ej: 0.8)</param>
    /// <param name="budgetModeMinProteinPerKg">Proteína mínima del modo (null si no aplica)</param>
    /// <param name="isOverridePlan">Si es plan médico con override</param>
    /// <param name="overrideMaxCarbsGrams">Máximo de carbs cuando hay override (ej: 60)</param>
    public KetoProfileResult Calculate(
        decimal weightKg,
        decimal heightCm,
        int age,
        Gender gender,
        ActivityLevel activityLevel,
        GoalType goalType,
        decimal bmrCalorieFloorFactor,
        decimal tdeeCalorieCeilingFactor,
        decimal minimumProteinPerKg,
        decimal? budgetModeMinProteinPerKg = null,
        bool isOverridePlan = false,
        int overrideMaxCarbsGrams = 60)
    {
        // ── 1. BMR Mifflin-St Jeor ────────────────────────────────────────────
        decimal bmr = CalculateBmr(weightKg, heightCm, age, gender);

        // ── 2. TDEE ───────────────────────────────────────────────────────────
        decimal tdee = bmr * TdeeMultipliers[activityLevel];

        // ── 3. Calorías diarias con piso/techo ───────────────────────────────
        decimal factor = CalorieFactors[goalType];
        decimal rawCalories = tdee * factor;

        decimal floorCalories  = bmr  * bmrCalorieFloorFactor;
        decimal ceilingCalories = tdee * tdeeCalorieCeilingFactor;

        decimal dailyCalories = Math.Clamp(rawCalories, floorCalories, ceilingCalories);

        // ── 4. Distribución de macros ─────────────────────────────────────────
        decimal effectiveMinProteinPerKg = budgetModeMinProteinPerKg ?? minimumProteinPerKg;

        decimal carbsCalories   = isOverridePlan ? 0m  : dailyCalories * 0.05m;
        decimal proteinCalories = dailyCalories * 0.25m;
        decimal fatCalories     = dailyCalories - carbsCalories - proteinCalories;

        decimal carbsGrams   = isOverridePlan
            ? Math.Min(dailyCalories * 0.05m / 4m, overrideMaxCarbsGrams)
            : carbsCalories / 4m;
        decimal proteinGrams = proteinCalories / 4m;
        decimal fatGrams     = fatCalories / 9m;

        // ── 5. Verificación de proteína mínima ───────────────────────────────
        decimal minProteinGrams = effectiveMinProteinPerKg * weightKg;
        if (proteinGrams < minProteinGrams)
        {
            // Ajustar proteína hacia arriba, compensar bajando grasa
            decimal deficit = (minProteinGrams - proteinGrams) * 4m; // calorías faltantes
            proteinGrams = minProteinGrams;
            fatGrams = (fatCalories - deficit) / 9m;
        }

        // ── 6. Ajuste override médico: redistribuir calorías de carbs extras ─
        if (isOverridePlan)
        {
            decimal carbsCaloriesActual = carbsGrams * 4m;
            decimal remainingCalories   = dailyCalories - carbsCaloriesActual;
            // Ratio proteína/grasa 30/70 del restante
            proteinGrams = (remainingCalories * 0.30m) / 4m;
            fatGrams     = (remainingCalories * 0.70m) / 9m;

            // Respetar proteína mínima
            if (proteinGrams < minProteinGrams)
            {
                decimal deficit = (minProteinGrams - proteinGrams) * 4m;
                proteinGrams = minProteinGrams;
                fatGrams = ((remainingCalories - deficit) * 0.70m) / 9m;
            }
        }

        // ── 7. Recalcular % para guardar ─────────────────────────────────────
        decimal totalCaloriesFromMacros = (carbsGrams * 4m) + (proteinGrams * 4m) + (fatGrams * 9m);
        decimal carbsPercent   = totalCaloriesFromMacros > 0 ? (carbsGrams * 4m   / totalCaloriesFromMacros) * 100m : 0m;
        decimal proteinPercent = totalCaloriesFromMacros > 0 ? (proteinGrams * 4m / totalCaloriesFromMacros) * 100m : 0m;
        decimal fatPercent     = totalCaloriesFromMacros > 0 ? (fatGrams * 9m     / totalCaloriesFromMacros) * 100m : 0m;

        return new KetoProfileResult
        {
            BmrKcal        = Math.Round(bmr, 1),
            TdeeKcal       = Math.Round(tdee, 1),
            DailyCalories  = (int)Math.Round(dailyCalories, 0),
            CarbsGrams     = Math.Round(carbsGrams, 1),
            ProteinGrams   = Math.Round(proteinGrams, 1),
            FatGrams       = Math.Round(fatGrams, 1),
            CarbsPercent   = Math.Round(carbsPercent, 1),
            ProteinPercent = Math.Round(proteinPercent, 1),
            FatPercent     = Math.Round(fatPercent, 1),
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PRIVATE
    // ─────────────────────────────────────────────────────────────────────────

    private static decimal CalculateBmr(decimal weightKg, decimal heightCm, int age, Gender gender)
    {
        // Fórmula Mifflin-St Jeor
        decimal baseBmr = (10m * weightKg) + (6.25m * heightCm) - (5m * age);

        return gender switch
        {
            Gender.Male               => baseBmr + 5m,
            Gender.Female             => baseBmr - 161m,
            Gender.NonBinary          => ((baseBmr + 5m) + (baseBmr - 161m)) / 2m,
            Gender.PreferNotToSay     => ((baseBmr + 5m) + (baseBmr - 161m)) / 2m,
            _                         => baseBmr - 78m,   // promedio como fallback
        };
    }
}
