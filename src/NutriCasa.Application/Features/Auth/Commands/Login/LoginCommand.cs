using MediatR;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Auth.DTOs;

namespace NutriCasa.Application.Features.Auth.Commands.Login;

public record LoginCommand : IRequest<Result<AuthTokenResponse>>
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}
