using System.Text.Json;

var tool = new RecipeGenerator();

// TODO: 1. Revisar recetas existentes en recipes-curated.json (evitar duplicados)
//       2. Llamar Gemini API con el prompt de generación
//       3. Validar cada receta con CuratedRecipeValidator
//       4. Fusionar nuevas recetas con el JSON existente
//       5. Guardar recipes-curated.json actualizado

var modeCodes = new[] { "economic", "pantry_basic", "simple_kitchen", "busy_parent", "athletic", "gourmet" };
var recipesPerMode = 15;

Console.WriteLine("=== NutriCasa Recipe Generator ===");
Console.WriteLine($"Target: {recipesPerMode} recipes × {modeCodes.Length} modes = {recipesPerMode * modeCodes.Length} recipes total");

var existingPath = tool.FindJsonPath();
if (existingPath != null)
{
    var existing = JsonSerializer.Deserialize<CuratedCatalog>(File.ReadAllText(existingPath));
    Console.WriteLine($"Existing recipes in JSON: {existing?.Recipes?.Count ?? 0}");
}
else
{
    Console.WriteLine("No existing recipes-curated.json found — will create new file.");
}

Console.WriteLine();
Console.WriteLine("Para generar las recetas, ejecutar con API key de Gemini:");
Console.WriteLine($"  dotnet run --project tools/NutriCasa.Tools -- --api-key AIza...");
Console.WriteLine();
Console.WriteLine("O configurar variable de entorno: NUTRICASA_GEMINI_KEY");
Console.WriteLine();
Console.WriteLine("Requerimientos:");
Console.WriteLine("  - NuGet: System.Net.Http.Json");
Console.WriteLine("  - Gemini 2.5 Pro API");
Console.WriteLine("  - ~$15-25 USD de costo estimado de API (una sola vez)");
Console.WriteLine("  - Validación automática post-generación");

if (args.Length > 0 && args[0] == "--api-key" && args.Length > 1)
{
    await tool.GenerateAsync(args[1], recipesPerMode, modeCodes, existingPath);
}
else
{
    var envKey = Environment.GetEnvironmentVariable("NUTRICASA_GEMINI_KEY");
    if (!string.IsNullOrEmpty(envKey))
    {
        Console.WriteLine("Usando NUTRICASA_GEMINI_KEY del entorno...");
        await tool.GenerateAsync(envKey, recipesPerMode, modeCodes, existingPath);
    }
}

internal sealed class RecipeGenerator
{
    private static readonly HttpClient Http = new();

    public string? FindJsonPath()
    {
        // Buscar en varias ubicaciones probables
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "NutriCasa.Infrastructure", "Persistence", "Seeds", "recipes-curated.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "src", "NutriCasa.Infrastructure", "Persistence", "Seeds", "recipes-curated.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "NutriCasa.Infrastructure", "Persistence", "Seeds", "recipes-curated.json"),
        };

        foreach (var path in candidates)
        {
            if (File.Exists(Path.GetFullPath(path)))
                return Path.GetFullPath(path);
        }

        return null;
    }

    public async Task GenerateAsync(string apiKey, int recipesPerMode, string[] modeCodes, string? outputPath)
    {
        Console.WriteLine("Iniciando generación con Gemini...");

        // TODO: Implementar llamada real a Gemini 2.5 Pro
        // var prompt = RecipeGenerationPrompt.Build(recipesPerMode);
        // var response = await CallGeminiAsync(apiKey, prompt);
        // var recipes = JsonSerializer.Deserialize<List<CuratedRecipeEntry>>(response);
        // Validar cada receta
        // Guardar resultado

        Console.WriteLine("Generación no implementada — requiere integración con Gemini API.");
        Console.WriteLine("Por ahora, editar recipes-curated.json manualmente o esperar herramienta completa.");
    }
}

internal sealed record CuratedCatalog
{
    public string? Version { get; init; }
    public string? Description { get; init; }
    public List<CuratedRecipeEntry>? Recipes { get; init; }
}

internal sealed record CuratedRecipeEntry
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
