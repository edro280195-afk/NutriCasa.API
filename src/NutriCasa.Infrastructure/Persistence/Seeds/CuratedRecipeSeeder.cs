using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Infrastructure.Persistence.Seeds;

public static class CuratedRecipeSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, ILogger? logger = null)
    {
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Persistence", "Seeds", "recipes-curated.json");
        if (!File.Exists(jsonPath))
        {
            jsonPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "..", "NutriCasa.Infrastructure", "Persistence", "Seeds", "recipes-curated.json");
        }

        if (!File.Exists(jsonPath))
        {
            logger?.LogWarning(
                "No se encontró recipes-curated.json (buscado en {Path}). El respaldo de recetas curadas quedará vacío.",
                jsonPath);
            return;
        }

        var json = await File.ReadAllTextAsync(jsonPath);
        var catalog = JsonSerializer.Deserialize<CuratedRecipeCatalog>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (catalog?.Recipes == null || catalog.Recipes.Count == 0) return;

        // Cargamos nombres Y slugs existentes para detectar duplicados
        // aunque los registros previos tengan Slug = null.
        var existingNames = await context.Recipes
            .Where(r => r.Source == RecipeSource.Curated)
            .Select(r => r.Name)
            .ToListAsync();
        var existingNamesSet = new HashSet<string>(
            existingNames.Where(n => n != null)!,
            StringComparer.OrdinalIgnoreCase);

        var existingSlugs = await context.Recipes
            .Where(r => r.Source == RecipeSource.Curated && r.Slug != null)
            .Select(r => r.Slug!)
            .ToListAsync();
        var existingSlugsSet = new HashSet<string>(existingSlugs, StringComparer.OrdinalIgnoreCase);

        static string Slugify(string name) => name.ToLowerInvariant()
            .Replace(" ", "-").Replace("á", "a").Replace("é", "e").Replace("í", "i")
            .Replace("ó", "o").Replace("ú", "u").Replace("ñ", "n");

        // Deduplicamos también dentro del catálogo por si dos entradas generan el mismo slug.
        var seenSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var recipes = new List<Recipe>();
        foreach (var r in catalog.Recipes)
        {
            var slug = r.Slug ?? Slugify(r.Name!);
            if (existingNamesSet.Contains(r.Name!)) continue;
            if (existingSlugsSet.Contains(slug)) continue;
            if (!seenSlugs.Add(slug)) continue;

            recipes.Add(new Recipe
        {
            Name = r.Name!,
            Slug = r.Slug ?? Slugify(r.Name!),
            Description = r.Description,
            MealType = Enum.Parse<MealType>(r.MealType!, true),
            NutritionTrack = NutritionTrack.Keto,
            Ingredients = r.Ingredients!,
            Instructions = r.Instructions,
            PrepTimeMin = r.PrepTimeMin,
            CookTimeMin = r.CookTimeMin,
            Servings = r.Servings,
            BaseCalories = r.BaseCalories,
            BaseProteinGr = r.BaseProteinGr,
            BaseFatGr = r.BaseFatGr,
            BaseCarbsGr = r.BaseCarbsGr,
            BaseNetCarbsGr = r.BaseNetCarbsGr,
            DifficultyScore = r.DifficultyScore,
            Tags = r.Tags ?? [],
            Source = RecipeSource.Curated,
            IsPublic = true,
            CompatibleModeCodes = r.CompatibleModeCodes ?? [],
            EconomicTier = r.EconomicTier,
            EstimatedCostPerServingMxn = r.EstimatedCostPerServingMxn,
            TotalPrepTimeMin = r.TotalPrepTimeMin,
            YieldServingsMin = r.YieldServingsMin,
            YieldServingsMax = r.YieldServingsMax,
            IsBatchCookable = r.IsBatchCookable,
            IsFreezable = r.IsFreezable,
            CookingMethods = r.CookingMethods ?? [],
            });
        }

        if (recipes.Count == 0)
        {
            logger?.LogInformation("Recetas curadas ya estaban sembradas; no se agregó ninguna nueva.");
            return;
        }

        context.Recipes.AddRange(recipes);
        await context.SaveChangesAsync();
        logger?.LogInformation("Sembradas {Count} recetas curadas de respaldo.", recipes.Count);
    }

    private sealed record CuratedRecipeCatalog
    {
        public string? Version { get; init; }
        public string? Description { get; init; }
        public List<CuratedRecipeEntry>? Recipes { get; init; }
    }

    private sealed record CuratedRecipeEntry
    {
        public string? Name { get; init; }
        public string? Slug { get; init; }
        public string? Description { get; init; }
        public string? MealType { get; init; }
        public string? Ingredients { get; init; }
        public string? Instructions { get; init; }
        public int? PrepTimeMin { get; init; }
        public int? CookTimeMin { get; init; }
        public int Servings { get; init; }
        public int BaseCalories { get; init; }
        public decimal BaseProteinGr { get; init; }
        public decimal BaseFatGr { get; init; }
        public decimal BaseCarbsGr { get; init; }
        public decimal? BaseNetCarbsGr { get; init; }
        public int? DifficultyScore { get; init; }
        public string[]? Tags { get; init; }
        public string[]? CompatibleModeCodes { get; init; }
        public int? EconomicTier { get; init; }
        public decimal? EstimatedCostPerServingMxn { get; init; }
        public int? TotalPrepTimeMin { get; init; }
        public int? YieldServingsMin { get; init; }
        public int? YieldServingsMax { get; init; }
        public bool IsBatchCookable { get; init; }
        public bool IsFreezable { get; init; }
        public string[]? CookingMethods { get; init; }
    }
}
