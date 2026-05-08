using MediatR;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Onboarding.DTOs;

namespace NutriCasa.Application.Features.Onboarding.Commands.CompleteStep7DisclaimerGoal;

public record CompleteStep7DisclaimerGoalCommand : IRequest<Result<CompleteStep7DisclaimerGoalResponse>>
{
    public Guid? DisclaimerVersionId { get; init; }
    public required string GoalType { get; init; }
    public decimal? TargetWeightKg { get; init; }
    public DateOnly? TargetDate { get; init; }
    public string? MotivationText { get; init; }
}
