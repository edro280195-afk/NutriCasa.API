namespace NutriCasa.Domain.ValueObjects;

/// <summary>
/// Macros cetogénicos objetivo de un usuario.
/// </summary>
public sealed record KetoMacros(
    int DailyCalories,
    decimal CarbsGrams,
    decimal ProteinGrams,
    decimal FatGrams,
    decimal? CarbsPercent = null,
    decimal? ProteinPercent = null,
    decimal? FatPercent = null);
