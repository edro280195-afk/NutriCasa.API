namespace NutriCasa.Domain.ValueObjects;

/// <summary>
/// Métricas corporales de una medición.
/// </summary>
public sealed record BodyMetrics(
    decimal WeightKg,
    decimal? BodyFatPercentage = null,
    decimal? WaistCm = null,
    decimal? HipCm = null,
    decimal? NeckCm = null,
    decimal? ArmCm = null,
    decimal? ChestCm = null,
    decimal? ThighCm = null);
