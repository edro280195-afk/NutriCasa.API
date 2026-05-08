using MediatR;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Measurements.DTOs;

namespace NutriCasa.Application.Features.Measurements.Commands.CreateMeasurement;

public record CreateMeasurementCommand : IRequest<Result<MeasurementResponse>>
{
    public required decimal WeightKg { get; init; }
    public decimal? BodyFatPercentage { get; init; }
    public decimal? WaistCm { get; init; }
    public decimal? HipCm { get; init; }
    public string? Notes { get; init; }
    public DateOnly? MeasuredAt { get; init; }
}
