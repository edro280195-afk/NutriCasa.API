using FluentValidation;

namespace NutriCasa.Application.Features.Onboarding.Commands.CompleteStep4BodyType;

public class CompleteStep4BodyTypeCommandValidator : AbstractValidator<CompleteStep4BodyTypeCommand>
{
    private static readonly string[] ValidBodyTypes = ["slim", "average", "athletic", "curvy", "plus", "heavy"];

    public CompleteStep4BodyTypeCommandValidator()
    {
        RuleFor(x => x.BodyType)
            .NotEmpty().WithMessage("El tipo de complexión es requerido.")
            .Must(v => ValidBodyTypes.Contains(v.ToLowerInvariant()))
            .WithMessage("Tipo de complexión inválido. Valores válidos: slim, average, athletic, curvy, plus, heavy.");
    }
}
