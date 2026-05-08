using MediatR;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.Auth.Commands.ResetPassword;

public record ResetPasswordCommand : IRequest<Result>
{
    public required string Token { get; init; }
    public required string NewPassword { get; init; }
}
