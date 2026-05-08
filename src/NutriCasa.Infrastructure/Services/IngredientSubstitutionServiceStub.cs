using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Infrastructure.Services;

public class IngredientSubstitutionServiceStub : IIngredientSubstitutionService
{
    public Task<List<SubstitutionSuggestion>> GetSubstitutionsAsync(string[] ingredients, string budgetModeCode, string[] userAllergies, CancellationToken ct = default)
        => throw new NotImplementedException("Migrado a IngredientSubstitutionService real. Eliminar este stub.");
}
