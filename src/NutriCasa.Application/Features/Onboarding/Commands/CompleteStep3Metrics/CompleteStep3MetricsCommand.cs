using MediatR;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Onboarding.DTOs;

namespace NutriCasa.Application.Features.Onboarding.Commands.CompleteStep3Metrics;

public record CompleteStep3MetricsCommand : IRequest<Result<CompleteStep3MetricsResponse>>
{
    public required decimal HeightCm { get; init; }
    public required decimal WeightKg { get; init; }
    public decimal? TargetWeightKg { get; init; }
    public string? GoalType { get; init; }
}
