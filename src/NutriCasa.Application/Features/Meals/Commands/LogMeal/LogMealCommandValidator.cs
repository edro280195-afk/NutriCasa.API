using FluentValidation;

namespace NutriCasa.Application.Features.Meals.Commands.LogMeal;

public class LogMealCommandValidator : AbstractValidator<LogMealCommand>
{
    public LogMealCommandValidator()
    {
        RuleFor(x => x.PlanMealId)
            .NotEmpty().WithMessage("El ID de la comida es requerido.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("El estado es requerido.")
            .Must(s => new[] { "completed", "partial", "skipped", "substituted" }.Contains(s.ToLowerInvariant()))
            .WithMessage("Estado inválido. Usa: completed, partial, skipped, substituted.");

        RuleFor(x => x.SubstitutionNote)
            .NotEmpty().When(x => x.Status.Equals("substituted", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Debes proporcionar una nota de sustitución.");

        RuleFor(x => x.ActualPortion)
            .InclusiveBetween(0.1m, 5.0m).When(x => x.ActualPortion.HasValue)
            .WithMessage("La porción debe estar entre 0.1 y 5.0.");
    }
}
