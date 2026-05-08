using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Infrastructure.Services;

public class IngredientSubstitutionService : IIngredientSubstitutionService
{
    private readonly IApplicationDbContext _context;

    public IngredientSubstitutionService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SubstitutionSuggestion>> GetSubstitutionsAsync(
        string[] ingredients, string budgetModeCode, string[] userAllergies, CancellationToken ct = default)
    {
        var substitutions = await _context.IngredientSubstitutions
            .Include(s => s.OriginalIngredient)
            .Include(s => s.Replacement)
            .Where(s => s.IsActive)
            .ToListAsync(ct);

        var suggestions = new List<SubstitutionSuggestion>();
        string modeLower = budgetModeCode.ToLowerInvariant();

        foreach (var ingredient in ingredients)
        {
            string ingLower = ingredient.ToLowerInvariant().Trim();

            var matchingSubs = substitutions
                .Where(s =>
                    s.ApplicableModeCodes.Any(m => m.ToLowerInvariant() == modeLower) &&
                    (s.OriginalIngredient.Name.ToLowerInvariant().Contains(ingLower) ||
                     ingLower.Contains(s.OriginalIngredient.Name.ToLowerInvariant())) &&
                    !userAllergies.Any(a => s.Replacement.Name.ToLowerInvariant().Contains(a.ToLowerInvariant())))
                .OrderByDescending(s => s.CostSavingsPercent)
                .ToList();

            foreach (var sub in matchingSubs.Where(s => s.CostSavingsPercent >= 20))
            {
                suggestions.Add(new SubstitutionSuggestion
                {
                    OriginalIngredientName = sub.OriginalIngredient.Name,
                    ReplacementIngredientName = sub.Replacement.Name,
                    ReplacementCode = sub.Replacement.Code,
                    CostSavingsPercent = sub.CostSavingsPercent ?? 0,
                    QualityLossScore = sub.QualityLossScore ?? 0,
                    Notes = sub.Notes,
                });
            }
        }

        return suggestions;
    }
}
