using FluentValidation;

namespace NutriCasa.Application.Features.Onboarding.Commands.CompleteStep6_5MedicalOverride;

public class CompleteStep6_5MedicalOverrideCommandValidator : AbstractValidator<CompleteStep6_5MedicalOverrideCommand>
{
    public CompleteStep6_5MedicalOverrideCommandValidator()
    {
        RuleFor(x => x.PasswordConfirmation)
            .NotEmpty().WithMessage("La confirmación de contraseña es requerida.");

        RuleFor(x => x.DisclaimerAccepted)
            .Equal(true).WithMessage("Debes aceptar el disclaimer médico.");

        RuleFor(x => x.DisclaimerVersionId)
            .NotEmpty().WithMessage("El disclaimer de override es requerido.");
    }
}
