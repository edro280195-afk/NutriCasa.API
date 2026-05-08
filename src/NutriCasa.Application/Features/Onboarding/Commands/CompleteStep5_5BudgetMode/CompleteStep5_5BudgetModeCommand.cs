using MediatR;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.Onboarding.Commands.CompleteStep5_5BudgetMode;

public record CompleteStep5_5BudgetModeCommand : IRequest<Result>
{
    public required Guid BudgetModeId { get; init; }
}
