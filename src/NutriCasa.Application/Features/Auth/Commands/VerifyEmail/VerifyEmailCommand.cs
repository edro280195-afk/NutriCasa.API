using MediatR;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Auth.DTOs;

namespace NutriCasa.Application.Features.Auth.Commands.VerifyEmail;

public record VerifyEmailCommand : IRequest<Result<AuthTokenResponse>>
{
    public required string Token { get; init; }
}
