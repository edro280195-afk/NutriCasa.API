using FluentValidation;

namespace NutriCasa.Application.Features.Onboarding.Commands.CompleteStep7DisclaimerGoal;

public class CompleteStep7DisclaimerGoalCommandValidator : AbstractValidator<CompleteStep7DisclaimerGoalCommand>
{
    private static readonly string[] ValidGoalTypes = ["WeightLoss", "BodyRecomp", "MuscleGain", "Maintenance", "Health"];

    public CompleteStep7DisclaimerGoalCommandValidator()
    {
        RuleFor(x => x.GoalType)
            .NotEmpty().WithMessage("El tipo de objetivo es requerido.")
            .Must(v => ValidGoalTypes.Contains(v))
            .WithMessage("Tipo de objetivo inválido.");
    }
}
