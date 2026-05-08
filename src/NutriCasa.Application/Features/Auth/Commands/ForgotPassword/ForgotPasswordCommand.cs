using MediatR;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand : IRequest<Result>
{
    public required string Email { get; init; }
}
