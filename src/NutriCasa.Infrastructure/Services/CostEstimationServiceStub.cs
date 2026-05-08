using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Infrastructure.Services;

public class CostEstimationServiceStub : ICostEstimationService
{
    public Task<PlanCostEstimate> EstimatePlanCostAsync(GeneratePlanResponse plan, string budgetModeCode, int numberOfPeople = 1, CancellationToken ct = default)
        => throw new NotImplementedException("Migrado a CostEstimationService real. Eliminar este stub.");
}
