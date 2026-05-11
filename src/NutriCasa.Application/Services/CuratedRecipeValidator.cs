using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Services;

/// <summary>
/// Valida recetas curadas contra reglas de dominio:
/// - Límites de macros por modo de presupuesto
/// - Consistencia de datos
/// - Net carbs vs total carbs
/// </summary>
public sealed class CuratedRecipeValidator : ICuratedRecipeValidator
{
    // Modos y sus códigos
    private static readonly string[] EconomicModes = ["economic"];
    private static readonly string[] StandardModes = ["economic", "pantry_basic"];
    private static readonly string[] AllModes = ["economic", "pantry_basic", "simple_kitchen", "busy_parent", "athletic", "gourmet"];

    public CuratedRecipeValidationResult Validate(CuratedRecipeValidationRequest request)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // ── Regla 1: Servings positivos ──
        if (request.Servings <= 0)
            errors.Add("Servings debe ser mayor a 0");

        // ── Regla 2: Modos compatibles válidos ──
        if (request.CompatibleModeCodes.Length == 0)
            errors.Add("Debe tener al menos un modo de presupuesto compatible");

        foreach (var mode in request.CompatibleModeCodes)
        {
            if (!AllModes.Contains(mode))
                errors.Add($"Modo '{mode}' no es un código válido");
        }

        // ── Regla 3: Rango de calorías ──
        if (request.BaseCalories <= 0)
            errors.Add("BaseCalories debe ser mayor a 0");
        else if (request.BaseCalories > 2000)
            warnings.Add("BaseCalories > 2000 es muy alto para una receta individual");

        // ── Regla 4: Macros por modo ──
        var isAthletic = request.CompatibleModeCodes.Contains("athletic");
        var isEconomic = request.CompatibleModeCodes.Any(c => EconomicModes.Contains(c));

        if (request.BaseProteinGr <= 0)
            errors.Add("BaseProteinGr debe ser mayor a 0");

        if (isAthletic && request.BaseProteinGr < 25)
            warnings.Add("Modo Atlético: proteína debería ser ≥ 25g por porción");

        if (request.BaseFatGr <= 0)
            errors.Add("BaseFatGr debe ser mayor a 0");

        // ── Regla 5: Net carbs vs Total carbs ──
        if (request.BaseNetCarbsGr.HasValue)
        {
            if (request.BaseNetCarbsGr > request.BaseCarbsGr)
                errors.Add("BaseNetCarbsGr no puede ser mayor que BaseCarbsGr");
        }

        if (request.BaseCarbsGr <= 0)
            warnings.Add("BaseCarbsGr en 0 — verificar si la receta tiene carbohidratos");

        // ── Regla 6: Macros por porción ──
        var calPerServing = request.Servings > 0 ? request.BaseCalories / request.Servings : 0;
        var protPerServing = request.Servings > 0 ? request.BaseProteinGr / request.Servings : 0;
        var carbsPerServing = request.Servings > 0 ? request.BaseCarbsGr / request.Servings : 0;

        if (calPerServing > 800)
            warnings.Add($"Calorías por porción ({calPerServing}) excede lo recomendado (máx 800)");

        if (carbsPerServing > 10)
            warnings.Add($"Carbohidratos netos por porción ({carbsPerServing:F1}g) — verificar si aplica para keto estándar");

        // ── Regla 7: Dificultad ──
        if (request.DifficultyScore.HasValue && (request.DifficultyScore < 1 || request.DifficultyScore > 5))
            errors.Add("DifficultyScore debe estar entre 1 y 5");

        return new CuratedRecipeValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings,
            CaloriesPerServing = calPerServing,
            ProteinPerServing = protPerServing,
            CarbsPerServing = carbsPerServing,
        };
    }
}
