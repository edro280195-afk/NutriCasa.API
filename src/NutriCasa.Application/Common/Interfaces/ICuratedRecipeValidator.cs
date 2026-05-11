using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Common.Interfaces;

/// <summary>
/// Valida recetas curadas (catálogo fijo) contra reglas de macros y modo de presupuesto.
/// Se usa para verificar que las recetas generadas por Gemini o escritas a mano
/// cumplan con los requisitos antes de insertarlas en la base de datos.
/// </summary>
public interface ICuratedRecipeValidator
{
    CuratedRecipeValidationResult Validate(CuratedRecipeValidationRequest request);
}

public record CuratedRecipeValidationRequest
{
    public required string RecipeName { get; init; }
    public required int Servings { get; init; }
    public required int BaseCalories { get; init; }
    public required decimal BaseProteinGr { get; init; }
    public required decimal BaseFatGr { get; init; }
    public required decimal BaseCarbsGr { get; init; }
    public decimal? BaseNetCarbsGr { get; init; }
    public required string[] CompatibleModeCodes { get; init; }
    public int? DifficultyScore { get; init; }
}

public record CuratedRecipeValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
    public int? CaloriesPerServing { get; init; }
    public decimal? ProteinPerServing { get; init; }
    public decimal? CarbsPerServing { get; init; }

    public string? Summary
    {
        get
        {
            if (IsValid) return $"✓ {Errors.Count} errores, {Warnings.Count} advertencias";
            return $"✗ {Errors.Count} errores, {Warnings.Count} advertencias";
        }
    }
}
