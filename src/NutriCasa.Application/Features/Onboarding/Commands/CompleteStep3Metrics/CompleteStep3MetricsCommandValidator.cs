using FluentValidation;

namespace NutriCasa.Application.Features.Onboarding.Commands.CompleteStep3Metrics;

public class CompleteStep3MetricsCommandValidator : AbstractValidator<CompleteStep3MetricsCommand>
{
    public CompleteStep3MetricsCommandValidator()
    {
        RuleFor(x => x.HeightCm)
            .InclusiveBetween(50, 250)
            .WithMessage("La altura debe estar entre 50 y 250 cm.");

        RuleFor(x => x.WeightKg)
            .InclusiveBetween(20, 300)
            .WithMessage("El peso debe estar entre 20 y 300 kg.");

        When(x => x.GoalType == "WeightLoss" && x.TargetWeightKg.HasValue, () =>
        {
            RuleFor(x => x.TargetWeightKg)
                .LessThan(x => x.WeightKg)
                .WithMessage("Para bajar de peso, tu meta debe ser menor a tu peso actual.");

            RuleFor(x => x.TargetWeightKg)
                .Must((command, targetWeight) =>
                {
                    decimal maxLoss = command.WeightKg * 0.50m;
                    return targetWeight >= (command.WeightKg - maxLoss);
                })
                .WithMessage("Meta demasiado agresiva (más del 50% de pérdida). Consulta con tu médico.");
        });
    }
}
