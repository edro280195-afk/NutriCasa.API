using FluentValidation;

namespace NutriCasa.Application.Features.Onboarding.Commands.CompleteStep1Group;

public class CompleteStep1GroupCommandValidator : AbstractValidator<CompleteStep1GroupCommand>
{
    public CompleteStep1GroupCommandValidator()
    {
        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("La acción es requerida.")
            .Must(a => a == "create" || a == "join").WithMessage("La acción debe ser 'create' o 'join'.");

        When(x => x.Action == "create", () =>
        {
            RuleFor(x => x.GroupName)
                .NotEmpty().WithMessage("El nombre del grupo es requerido para crear.")
                .Length(3, 100).WithMessage("El nombre del grupo debe tener entre 3 y 100 caracteres.");
        });

        When(x => x.Action == "join", () =>
        {
            RuleFor(x => x.InviteCode)
                .NotEmpty().WithMessage("El código de invitación es requerido para unirse.");
        });
    }
}
