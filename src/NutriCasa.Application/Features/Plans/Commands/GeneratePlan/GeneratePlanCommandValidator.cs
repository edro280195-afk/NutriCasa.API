using FluentValidation;

namespace NutriCasa.Application.Features.Plans.Commands.GeneratePlan;

public class GeneratePlanCommandValidator : AbstractValidator<GeneratePlanCommand>
{
    public GeneratePlanCommandValidator()
    {
        RuleFor(x => x.WeekStartDate)
            .NotEmpty().WithMessage("La fecha de inicio de la semana es requerida.");
    }
}
