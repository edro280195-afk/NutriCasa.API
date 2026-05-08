namespace NutriCasa.Application.Features.Measurements.DTOs;

public record CreateMeasurementRequest
{
    public required decimal WeightKg { get; init; }
    public decimal? BodyFatPercentage { get; init; }
    public decimal? WaistCm { get; init; }
    public decimal? HipCm { get; init; }
    public string? Notes { get; init; }
    public DateOnly? MeasuredAt { get; init; }
}

public record MeasurementResponse
{
    public required Guid Id { get; init; }
    public required decimal WeightKg { get; init; }
    public decimal? BodyFatPercentage { get; init; }
    public decimal? WaistCm { get; init; }
    public decimal? HipCm { get; init; }
    public string? Notes { get; init; }
    public required DateOnly MeasuredAt { get; init; }
    public required DateTime CreatedAt { get; init; }
}
