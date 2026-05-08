using FluentValidation;

namespace NutriCasa.Application.Features.Onboarding.Commands.CompleteStep5Activity;

public class CompleteStep5ActivityCommandValidator : AbstractValidator<CompleteStep5ActivityCommand>
{
    private static readonly string[] ValidLevels = ["Sedentary", "Light", "Moderate", "Active", "VeryActive"];

    public CompleteStep5ActivityCommandValidator()
    {
        RuleFor(x => x.ActivityLevel)
            .NotEmpty().WithMessage("El nivel de actividad es requerido.")
            .Must(v => ValidLevels.Contains(v))
            .WithMessage("Nivel de actividad inválido. Valores: Sedentary, Light, Moderate, Active, VeryActive.");
    }
}
