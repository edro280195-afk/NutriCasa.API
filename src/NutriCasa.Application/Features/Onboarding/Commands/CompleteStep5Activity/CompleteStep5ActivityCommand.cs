using MediatR;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.Onboarding.Commands.CompleteStep5Activity;

public record CompleteStep5ActivityCommand : IRequest<Result>
{
    public required string ActivityLevel { get; init; }
}
