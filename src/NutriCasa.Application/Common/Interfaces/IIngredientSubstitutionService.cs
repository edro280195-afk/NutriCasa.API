namespace NutriCasa.Application.Common.Interfaces;

public interface IIngredientSubstitutionService
{
    Task<string?> FindSubstitutionAsync(string ingredientCode, string modeCode, CancellationToken ct = default);
}
