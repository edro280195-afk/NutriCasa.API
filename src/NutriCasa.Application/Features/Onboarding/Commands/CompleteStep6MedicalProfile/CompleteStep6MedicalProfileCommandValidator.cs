using FluentValidation;

namespace NutriCasa.Application.Features.Onboarding.Commands.CompleteStep6MedicalProfile;

public class CompleteStep6MedicalProfileCommandValidator : AbstractValidator<CompleteStep6MedicalProfileCommand>
{
    private static readonly string[] ValidDiabetesTypes = ["T1", "T2", "Gestational"];
    private static readonly string[] ValidKetoLevels = ["Beginner", "Intermediate", "Advanced"];

    public CompleteStep6MedicalProfileCommandValidator()
    {
        When(x => x.HasDiabetes, () =>
        {
            RuleFor(x => x.DiabetesType)
                .NotEmpty().WithMessage("El tipo de diabetes es requerido.")
                .Must(v => ValidDiabetesTypes.Contains(v))
                .WithMessage("Tipo de diabetes inválido.");
        });

        RuleFor(x => x.KetoExperienceLevel)
            .NotEmpty().WithMessage("El nivel de experiencia keto es requerido.")
            .Must(v => ValidKetoLevels.Contains(v))
            .WithMessage("Nivel de experiencia keto inválido.");
    }
}
