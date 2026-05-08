using MediatR;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.Plans.Commands.GeneratePlan;

public record GeneratePlanCommand : IRequest<Result<PlanGenerationResult>>
{
    public required DateOnly WeekStartDate { get; init; }
    public bool ForceRegenerate { get; init; }
}
