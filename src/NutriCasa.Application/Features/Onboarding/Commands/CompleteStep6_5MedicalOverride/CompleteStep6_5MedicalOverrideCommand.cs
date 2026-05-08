using MediatR;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.Onboarding.Commands.CompleteStep6_5MedicalOverride;

public record CompleteStep6_5MedicalOverrideCommand : IRequest<Result>
{
    public required string PasswordConfirmation { get; init; }
    public required bool DisclaimerAccepted { get; init; }
    public required Guid DisclaimerVersionId { get; init; }
}
