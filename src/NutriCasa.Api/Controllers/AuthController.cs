using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriCasa.Application.Features.Auth.Commands.ForgotPassword;
using NutriCasa.Application.Features.Auth.Commands.Login;
using NutriCasa.Application.Features.Auth.Commands.Logout;
using NutriCasa.Application.Features.Auth.Commands.RefreshToken;
using NutriCasa.Application.Features.Auth.Commands.RegisterUser;
using NutriCasa.Application.Features.Auth.Commands.ResetPassword;
using NutriCasa.Application.Features.Auth.Commands.VerifyEmail;
using NutriCasa.Application.Features.Auth.DTOs;
using NutriCasa.Application.Features.Auth.Queries.GetCurrentUser;

namespace NutriCasa.Api.Controllers;

public class AuthController : BaseApiController
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, CancellationToken ct)
    {
        var command = new RegisterUserCommand
        {
            FullName = request.FullName,
            Email = request.Email,
            Password = request.Password,
            BirthDate = request.BirthDate
        };

        var result = await _mediator.Send(command, ct);
        return HandleResult(result);
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token, CancellationToken ct)
    {
        var command = new VerifyEmailCommand { Token = token };
        var result = await _mediator.Send(command, ct);
        return HandleResult(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var command = new LoginCommand
        {
            Email = request.Email,
            Password = request.Password
        };

        var result = await _mediator.Send(command, ct);
        return HandleResult(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var command = new RefreshTokenCommand { RefreshToken = request.RefreshToken };
        var result = await _mediator.Send(command, ct);
        return HandleResult(result);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        var command = new ForgotPasswordCommand { Email = request.Email };
        var result = await _mediator.Send(command, ct);
        return HandleResult(result);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var command = new ResetPasswordCommand
        {
            Token = request.Token,
            NewPassword = request.NewPassword
        };

        var result = await _mediator.Send(command, ct);
        return HandleResult(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
    {
        var command = new LogoutCommand { RefreshToken = request.RefreshToken };
        var result = await _mediator.Send(command, ct);
        return HandleResult(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        var query = new GetCurrentUserQuery();
        var result = await _mediator.Send(query, ct);
        return HandleResult(result);
    }
}
