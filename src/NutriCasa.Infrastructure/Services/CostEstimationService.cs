using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Infrastructure.Services;

public class CostEstimationService : ICostEstimationService
{
    private readonly IApplicationDbContext _context;

    public CostEstimationService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PlanCostEstimate> EstimatePlanCostAsync(
        GeneratePlanResponse plan, string budgetModeCode, int numberOfPeople = 1, CancellationToken ct = default)
    {
        var allIngredients = plan.Days
            .SelectMany(d => d.Meals)
            .SelectMany(m => m.Ingredients)
            .ToList();

        var uniqueNames = allIngredients
            .Select(i => i.Name.ToLowerInvariant().Trim())
            .Distinct()
            .ToArray();

        decimal totalCost = 0;
        var costByCategory = new Dictionary<string, decimal>();
        var costByStore = new Dictionary<string, decimal>();

        var catalog = await _context.IngredientCatalog
            .Where(i => i.IsActive)
            .ToListAsync(ct);

        int foundCount = 0;

        foreach (var ingName in uniqueNames)
        {
            var match = catalog.FirstOrDefault(c =>
                (c.NameSearch != null && ingName.Contains(c.NameSearch.ToLowerInvariant())) ||
                ingName.Contains(c.Name.ToLowerInvariant()));

            if (match?.AvgPriceMxn > 0)
            {
                var totalAmount = allIngredients
                    .Where(i => i.Name.ToLowerInvariant().Trim() == ingName)
                    .Sum(i => i.AmountGr);

                decimal proportion = totalAmount / (match.UnitGramsEquivalent ?? 100m);
                decimal cost = proportion * match.AvgPriceMxn.Value;
                totalCost += cost;

                string category = match.Category ?? "Otros";
                if (costByCategory.ContainsKey(category))
                    costByCategory[category] += cost;
                else
                    costByCategory[category] = cost;

                string store = match.PrimaryStoreCategory ?? "supermercado";
                if (costByStore.ContainsKey(store))
                    costByStore[store] += cost;
                else
                    costByStore[store] = cost;

                foundCount++;
            }
        }

        decimal gourmetFactor = budgetModeCode.ToLowerInvariant() switch
        {
            "economic" => 3.5m,
            "pantry_basic" => 2.5m,
            "simple_kitchen" => 2.5m,
            "busy_parent" => 2.0m,
            "athletic" => 1.8m,
            "gourmet" => 1.0m,
            _ => 2.0m,
        };

        decimal gourmetBaseline = totalCost * gourmetFactor;
        decimal savings = gourmetBaseline - totalCost;
        decimal savingsPercent = gourmetBaseline > 0 ? (savings / gourmetBaseline) * 100 : 0;

        return new PlanCostEstimate
        {
            TotalCostMxn = Math.Round(totalCost, 2),
            CostPerPersonMxn = Math.Round(totalCost / numberOfPeople, 2),
            GourmetBaselineCostMxn = Math.Round(gourmetBaseline, 2),
            SavingsVsGourmetMxn = Math.Round(savings, 2),
            SavingsVsGourmetPercent = Math.Round(savingsPercent, 2),
            CostByCategory = costByCategory,
            CostByStore = costByStore,
        };
    }
}
