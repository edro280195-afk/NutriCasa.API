using NutriCasa.Application.Common.Interfaces;

namespace NutriCasa.Infrastructure.Services;

/// <summary>
/// Pendiente de Fase 1 — Sustitución por modo.
/// </summary>
public class IngredientSubstitutionServiceStub : IIngredientSubstitutionService
{
    public Task<string?> FindSubstitutionAsync(string ingredientCode, string modeCode, CancellationToken ct = default)
        => throw new NotImplementedException("Pendiente de Fase 1 — Sustitución por modo");
}
