namespace NutriCasa.Application.Common.Interfaces;

public interface ICostEstimationService
{
    Task<decimal> EstimateRecipeCostAsync(string ingredientsJson, string modeCode, CancellationToken ct = default);
    Task<decimal> EstimateWeeklyPlanCostAsync(string planJson, string modeCode, CancellationToken ct = default);
}
