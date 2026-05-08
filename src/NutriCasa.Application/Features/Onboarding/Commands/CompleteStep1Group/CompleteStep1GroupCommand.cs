using MediatR;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Onboarding.DTOs;

namespace NutriCasa.Application.Features.Onboarding.Commands.CompleteStep1Group;

public record CompleteStep1GroupCommand : IRequest<Result<CompleteStep1GroupResponse>>
{
    public required string Action { get; init; } // "create" | "join"
    public string? GroupName { get; init; }
    public string? InviteCode { get; init; }
}
