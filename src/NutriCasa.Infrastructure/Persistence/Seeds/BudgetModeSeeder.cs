using Microsoft.EntityFrameworkCore;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Infrastructure.Persistence.Seeds;

public static class BudgetModeSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.BudgetModes.AnyAsync()) return;
        context.BudgetModes.AddRange(
            new BudgetMode { Code = "economic", Name = "Económico", ShortDescription = "Máximo sabor, mínimo costo", IconCode = "savings", ColorTheme = "#4CAF50", SortOrder = 1, EstimatedCostMinMxn = 400, EstimatedCostMaxMxn = 550, Rules = "{\"max_cost_per_meal_mxn\":35,\"max_cost_per_week_mxn\":550,\"max_prep_time_min\":45,\"allowed_proteins\":[\"huevo\",\"pollo_entero\",\"atun_lata\",\"sardina_lata\",\"carne_molida\",\"cerdo\",\"queso_panela\",\"queso_fresco\"],\"banned_proteins\":[\"salmon\",\"robalo\",\"camaron\",\"atun_fresco\",\"ribeye\",\"lamb\",\"pato\"],\"preferred_stores\":[\"mercado_tradicional\",\"tianguis\"]}" },
            new BudgetMode { Code = "pantry_basic", Name = "Despensa Básica", ShortDescription = "Cocina familiar cetogénica", IconCode = "kitchen", ColorTheme = "#FF9800", SortOrder = 2, EstimatedCostMinMxn = 550, EstimatedCostMaxMxn = 750, Rules = "{\"max_cost_per_meal_mxn\":50,\"max_cost_per_week_mxn\":750,\"max_prep_time_min\":50,\"preferred_stores\":[\"supermercado\",\"mercado_tradicional\"]}" },
            new BudgetMode { Code = "simple_kitchen", Name = "Cocina Simple", ShortDescription = "Mínimo tiempo, máxima practicidad", IconCode = "timer", ColorTheme = "#2196F3", SortOrder = 3, EstimatedCostMinMxn = 550, EstimatedCostMaxMxn = 750, Rules = "{\"max_cost_per_meal_mxn\":50,\"max_cost_per_week_mxn\":750,\"max_prep_time_min\":20,\"max_ingredients_per_recipe\":6,\"allowed_cooking_methods\":[\"stovetop\",\"microwave\",\"blender\",\"no_cook\"]}" },
            new BudgetMode { Code = "busy_parent", Name = "Papá/Mamá Ocupado", ShortDescription = "Batch cooking para toda la semana", IconCode = "family_restroom", ColorTheme = "#9C27B0", SortOrder = 4, EstimatedCostMinMxn = 700, EstimatedCostMaxMxn = 900, Rules = "{\"max_cost_per_meal_mxn\":60,\"max_cost_per_week_mxn\":900,\"min_batch_recipes\":3,\"min_batch_servings\":4,\"sunday_prep_max_hours\":2}" },
            new BudgetMode { Code = "athletic", Name = "Atlético", ShortDescription = "Alto en proteína, optimizado para rendimiento", IconCode = "fitness_center", ColorTheme = "#F44336", SortOrder = 5, EstimatedCostMinMxn = 900, EstimatedCostMaxMxn = 1200, Rules = "{\"max_cost_per_meal_mxn\":80,\"max_cost_per_week_mxn\":1200,\"min_protein_per_kg\":1.6,\"min_protein_per_meal_g\":25,\"min_shakes_per_week\":4}" },
            new BudgetMode { Code = "gourmet", Name = "Gourmet", ShortDescription = "Variedad internacional premium", IconCode = "restaurant", ColorTheme = "#795548", SortOrder = 6, EstimatedCostMinMxn = 1400, EstimatedCostMaxMxn = 1800, Rules = "{\"max_cost_per_meal_mxn\":150,\"max_cost_per_week_mxn\":1800,\"max_prep_time_min\":90,\"min_distinct_cuisines\":3,\"min_distinct_proteins\":6}" }
        );
        await context.SaveChangesAsync();
    }
}
