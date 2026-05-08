using MediatR;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Auth.DTOs;

namespace NutriCasa.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand : IRequest<Result<AuthTokenResponse>>
{
    public required string RefreshToken { get; init; }
}
