using FluentValidation;

namespace NutriCasa.Application.Features.CheckIns.Commands.CreateCheckIn;

public class CreateCheckInCommandValidator : AbstractValidator<CreateCheckInCommand>
{
    public CreateCheckInCommandValidator()
    {
        When(x => x.HungerLevel.HasValue, () =>
        {
            RuleFor(x => x.HungerLevel)
                .InclusiveBetween(1, 10)
                .WithMessage("El nivel de hambre debe estar entre 1 y 10.");
        });

        When(x => x.EnergyLevel.HasValue, () =>
        {
            RuleFor(x => x.EnergyLevel)
                .InclusiveBetween(1, 10)
                .WithMessage("El nivel de energía debe estar entre 1 y 10.");
        });

        When(x => x.MoodLevel.HasValue, () =>
        {
            RuleFor(x => x.MoodLevel)
                .InclusiveBetween(1, 10)
                .WithMessage("El estado de ánimo debe estar entre 1 y 10.");
        });

        When(x => x.SleepHours.HasValue, () =>
        {
            RuleFor(x => x.SleepHours)
                .InclusiveBetween(0, 24)
                .WithMessage("Las horas de sueño deben estar entre 0 y 24.");
        });

        When(x => x.WaterLiters.HasValue, () =>
        {
            RuleFor(x => x.WaterLiters)
                .InclusiveBetween(0, 15)
                .WithMessage("Los litros de agua deben estar entre 0 y 15.");
        });
    }
}
