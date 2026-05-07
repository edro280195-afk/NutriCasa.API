using NutriCasa.Application.Common.Interfaces;

namespace NutriCasa.Infrastructure.Services;

/// <summary>
/// Pendiente de Fase 1 — Estimación de costos por receta y plan.
/// </summary>
public class CostEstimationServiceStub : ICostEstimationService
{
    public Task<decimal> EstimateRecipeCostAsync(string ingredientsJson, string modeCode, CancellationToken ct = default)
        => throw new NotImplementedException("Pendiente de Fase 1 — Estimación de costos por receta y plan");

    public Task<decimal> EstimateWeeklyPlanCostAsync(string planJson, string modeCode, CancellationToken ct = default)
        => throw new NotImplementedException("Pendiente de Fase 1 — Estimación de costos por receta y plan");
}
