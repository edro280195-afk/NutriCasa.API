using FluentValidation;

namespace NutriCasa.Application.Features.Auth.Commands.RegisterUser;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MinimumLength(2).WithMessage("El nombre debe tener al menos 2 caracteres.")
            .MaximumLength(150).WithMessage("El nombre no puede exceder 150 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es requerido.")
            .EmailAddress().WithMessage("El email no tiene un formato válido.")
            .MaximumLength(255).WithMessage("El email no puede exceder 255 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es requerida.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.");
        // NIST 800-63B: sin reglas arbitrarias de mayúsculas, símbolos, etc.

        RuleFor(x => x.BirthDate)
            .NotEmpty().WithMessage("La fecha de nacimiento es requerida.")
            .Must(BeAtLeast18YearsOld)
            .WithMessage("Debes tener al menos 18 años para registrarte en NutriCasa.");
    }

    private static bool BeAtLeast18YearsOld(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var eighteenYearsAgo = today.AddYears(-18);
        return birthDate <= eighteenYearsAgo;
    }
}
