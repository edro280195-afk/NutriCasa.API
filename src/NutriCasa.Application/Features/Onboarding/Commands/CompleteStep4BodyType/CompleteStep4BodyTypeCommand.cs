using MediatR;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.Onboarding.Commands.CompleteStep4BodyType;

public record CompleteStep4BodyTypeCommand : IRequest<Result>
{
    public required string BodyType { get; init; }
}
