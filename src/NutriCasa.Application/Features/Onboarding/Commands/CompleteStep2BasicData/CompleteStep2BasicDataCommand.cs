using MediatR;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.Onboarding.Commands.CompleteStep2BasicData;

public record CompleteStep2BasicDataCommand : IRequest<Result>
{
    public string? FullName { get; init; }
    public string? ProfilePhotoUrl { get; init; }
    public required DateOnly BirthDate { get; init; }
    public required string Gender { get; init; } // "Male", "Female", "NonBinary", "PreferNotToSay"
}
