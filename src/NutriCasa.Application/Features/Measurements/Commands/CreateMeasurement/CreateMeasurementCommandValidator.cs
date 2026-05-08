using FluentValidation;

namespace NutriCasa.Application.Features.Measurements.Commands.CreateMeasurement;

public class CreateMeasurementCommandValidator : AbstractValidator<CreateMeasurementCommand>
{
    public CreateMeasurementCommandValidator()
    {
        RuleFor(x => x.WeightKg)
            .InclusiveBetween(20, 400)
            .WithMessage("El peso debe estar entre 20 y 400 kg.");

        When(x => x.BodyFatPercentage.HasValue, () =>
        {
            RuleFor(x => x.BodyFatPercentage)
                .InclusiveBetween(3, 70)
                .WithMessage("El porcentaje de grasa debe estar entre 3% y 70%.");
        });

        When(x => x.WaistCm.HasValue, () =>
        {
            RuleFor(x => x.WaistCm)
                .InclusiveBetween(30, 200)
                .WithMessage("La cintura debe estar entre 30 y 200 cm.");
        });
    }
}
