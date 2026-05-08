using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Common.Interfaces;

/// <summary>
/// Estima el costo monetario de un plan semanal usando el catálogo de ingredientes.
/// </summary>
public interface ICostEstimationService
{
    Task<PlanCostEstimate> EstimatePlanCostAsync(
        GeneratePlanResponse plan,
        string budgetModeCode,
        int numberOfPeople = 1,
        CancellationToken ct = default);
}
