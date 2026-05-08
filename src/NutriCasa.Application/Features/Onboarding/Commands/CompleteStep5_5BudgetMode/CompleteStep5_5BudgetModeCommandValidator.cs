using FluentValidation;

namespace NutriCasa.Application.Features.Onboarding.Commands.CompleteStep5_5BudgetMode;

public class CompleteStep5_5BudgetModeCommandValidator : AbstractValidator<CompleteStep5_5BudgetModeCommand>
{
    public CompleteStep5_5BudgetModeCommandValidator()
    {
        RuleFor(x => x.BudgetModeId)
            .NotEmpty().WithMessage("El modo de presupuesto es requerido.");
    }
}
