using FluentValidation;

namespace NutriCasa.Application.Features.Onboarding.Commands.CompleteStep2BasicData;

public class CompleteStep2BasicDataCommandValidator : AbstractValidator<CompleteStep2BasicDataCommand>
{
    public CompleteStep2BasicDataCommandValidator()
    {
        RuleFor(x => x.Gender)
            .NotEmpty().WithMessage("El género es requerido.")
            .Must(g => g == "Male" || g == "Female" || g == "NonBinary" || g == "PreferNotToSay")
            .WithMessage("El género proporcionado no es válido.");

        RuleFor(x => x.BirthDate)
            .NotEmpty().WithMessage("La fecha de nacimiento es requerida.")
            .Must(BeAtLeast18YearsOld)
            .WithMessage("Debes tener al menos 18 años.");

        When(x => !string.IsNullOrEmpty(x.FullName), () =>
        {
            RuleFor(x => x.FullName)
                .MinimumLength(2).WithMessage("El nombre debe tener al menos 2 caracteres.")
                .MaximumLength(150).WithMessage("El nombre no puede exceder 150 caracteres.");
        });
    }

    private static bool BeAtLeast18YearsOld(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var eighteenYearsAgo = today.AddYears(-18);
        return birthDate <= eighteenYearsAgo;
    }
}
