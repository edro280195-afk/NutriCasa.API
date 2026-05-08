using FluentValidation;

namespace NutriCasa.Application.Features.Auth.Commands.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es requerido.")
            .EmailAddress().WithMessage("El email no tiene un formato válido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es requerida.");
    }
}
