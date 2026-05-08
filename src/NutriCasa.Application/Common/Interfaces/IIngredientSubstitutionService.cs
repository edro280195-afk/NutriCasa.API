using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Common.Interfaces;

/// <summary>
/// Busca sustituciones de ingredientes por modo de presupuesto y alergias del usuario.
/// </summary>
public interface IIngredientSubstitutionService
{
    Task<List<SubstitutionSuggestion>> GetSubstitutionsAsync(
        string[] ingredients,
        string budgetModeCode,
        string[] userAllergies,
        CancellationToken ct = default);
}
