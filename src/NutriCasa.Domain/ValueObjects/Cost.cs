namespace NutriCasa.Domain.ValueObjects;

/// <summary>
/// Representación de un costo en MXN.
/// </summary>
public sealed record Cost(
    decimal AmountMxn,
    string? Description = null);
