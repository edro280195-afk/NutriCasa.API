using Microsoft.EntityFrameworkCore;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Infrastructure.Persistence.Seeds;

public static class IngredientSubstitutionSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.IngredientSubstitutions.AnyAsync()) return;
        var ingredients = await context.IngredientCatalog.ToDictionaryAsync(i => i.Code, i => i.Id);
        if (ingredients.Count == 0) return;

        var subs = new List<IngredientSubstitution>();
        void Add(string orig, string repl, string[] modes, SubstitutionReason reason, decimal? savings, int? quality, string? notes)
        {
            if (ingredients.TryGetValue(orig, out var origId) && ingredients.TryGetValue(repl, out var replId))
                subs.Add(new IngredientSubstitution { OriginalIngredientId = origId, ReplacementId = replId, ApplicableModeCodes = modes, Reason = reason, CostSavingsPercent = savings, QualityLossScore = quality, Notes = notes });
        }

        Add("salmon_fillet", "sardines_can", ["economic", "pantry_basic"], SubstitutionReason.Cost, 95, 1, "Las sardinas en lata son una excelente fuente de omega-3 y son 95% más económicas que el salmón fresco");
        Add("almond_flour_premium", "linaza", ["economic"], SubstitutionReason.Cost, 85, 2, "Linaza molida funciona como sustituto en muchas recetas keto a un fragmento del costo");
        Add("grass_fed_butter", "manteca_cerdo", ["economic", "pantry_basic"], SubstitutionReason.Cost, 70, 1, "La manteca de cerdo tradicional mexicana es nutricionalmente comparable y mucho más económica");
        Add("macadamia", "cacahuates", ["economic", "pantry_basic"], SubstitutionReason.Cost, 88, 2, "Los cacahuates son keto-amigables (en moderación) y mucho más accesibles");
        Add("exotic_cheese", "queso_panela", ["economic", "pantry_basic"], SubstitutionReason.Cost, 67, 2, "El queso panela es la base perfecta de la cocina mexicana keto, alta proteína y costo accesible");
        Add("mct_oil", "aceite_coco", ["economic", "pantry_basic", "simple_kitchen", "busy_parent"], SubstitutionReason.Cost, 75, 2, "El aceite de coco contiene MCTs naturales y es funcional para keto a una fracción del costo");
        Add("chicken_breast", "chicken_thighs", ["economic", "busy_parent"], SubstitutionReason.Cost, 32, 1, "El muslo de pollo es más económico, tiene más sabor y resulta mejor para batch cooking");
        Add("prawn", "tuna_can", ["economic", "pantry_basic"], SubstitutionReason.Cost, 96, 3, "Para recetas tipo coctel o ensaladas, el atún en lata es alternativa accesible");

        context.IngredientSubstitutions.AddRange(subs);
        await context.SaveChangesAsync();
    }
}
