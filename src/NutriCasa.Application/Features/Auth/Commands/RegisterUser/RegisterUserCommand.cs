using MediatR;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Auth.DTOs;

namespace NutriCasa.Application.Features.Auth.Commands.RegisterUser;

public record RegisterUserCommand : IRequest<Result<RegisterResponse>>
{
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required DateOnly BirthDate { get; init; }
}
