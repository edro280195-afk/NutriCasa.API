using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Infrastructure.Services;

public class PlanValidatorStub : IPlanValidator
{
    public PlanValidationResult Validate(GeneratePlanResponse plan, PlanValidationContext context)
        => throw new NotImplementedException("Migrado a PlanValidator real. Eliminar este stub.");
}
