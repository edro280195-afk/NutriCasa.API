using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Domain.Constants;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Infrastructure.Services;

public class GeminiService : IGeminiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GeminiService> _logger;

    private const decimal InputPricePerToken = 0.0000035m;
    private const decimal OutputPricePerToken = 0.0000105m;

    public GeminiService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IApplicationDbContext context,
        IMemoryCache cache,
        ILogger<GeminiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<GeneratePlanResponse> GeneratePlanAsync(GeneratePlanRequest request, CancellationToken cancellationToken = default)
    {
        if (IsCircuitBreakerOpen())
            throw new InvalidOperationException("Circuit breaker abierto. Usar fallback.");

        string requestHash = ComputeSha256Hash(JsonSerializer.Serialize(request));

        var cachedResponse = await CheckCacheAsync(requestHash, cancellationToken);
        if (cachedResponse is not null)
        {
            _logger.LogInformation("Cache hit para plan del usuario {UserId}", request.UserId);
            return cachedResponse;
        }

        string promptTemplate = GetPromptTemplate(request.BudgetModeCode);
        string prompt = BuildPrompt(promptTemplate, request);

        string? lastError = null;
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var response = await CallGeminiAsync(prompt, request.IsOverridePlan, cancellationToken);

                await LogInteractionAsync(request, requestHash, prompt, response.raw, attempt, response.inputTokens, response.outputTokens, true, null, cancellationToken);
                _logger.LogInformation("Gemini plan generado para usuario {UserId} (intento {Attempt})", request.UserId, attempt);

                RecordSuccess();

                return response.plan;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                _logger.LogWarning("Intento {Attempt} falló para Gemini: {Error}", attempt, ex.Message);

                if (attempt < 3)
                {
                    prompt = BuildCorrectivePrompt(promptTemplate, request, lastError);
                    await Task.Delay(1000 * attempt, cancellationToken);
                }
            }
        }

        await LogInteractionAsync(request, requestHash, prompt, lastError!, 3, 0, 0, false, lastError, cancellationToken);
        RecordFailure();
        throw new InvalidOperationException($"Gemini falló tras 3 intentos: {lastError}");
    }

    public Task<SwapMealResponse> SwapMealAsync(SwapMealRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Swap de comidas se implementa en Fase 2.");
    }

    private async Task<(GeneratePlanResponse plan, string raw, int inputTokens, int outputTokens)> CallGeminiAsync(string prompt, bool isOverride, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("Gemini");
        string model = _configuration["Gemini:PlanModel"] ?? "gemini-2.5-pro-preview-05-06";
        string apiKey = _configuration["Gemini:ApiKey"]!;

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature = 0.7,
                maxOutputTokens = 8192,
                responseMimeType = "application/json"
            }
        };

        var response = await client.PostAsJsonAsync(
            $"{model}:generateContent?key={apiKey}",
            requestBody, ct);

        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(jsonResponse);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "{}";

        int inputTokens = prompt.Length / 4;
        int outputTokens = text.Length / 4;

        var plan = JsonSerializer.Deserialize<GeneratePlanResponse>(text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (plan is null)
            throw new InvalidOperationException("La respuesta de Gemini no pudo ser deserializada.");

        plan = plan with { RawJson = text };

        return (plan, text, inputTokens, outputTokens);
    }

    private string BuildPrompt(string template, GeneratePlanRequest request)
    {
        return template
            .Replace("{{user_name}}", request.UserName)
            .Replace("{{age}}", request.Age.ToString())
            .Replace("{{gender}}", request.Gender)
            .Replace("{{height_cm}}", request.HeightCm.ToString("F1"))
            .Replace("{{weight_kg}}", request.WeightKg.ToString("F1"))
            .Replace("{{target_weight_kg}}", request.TargetWeightKg?.ToString("F1") ?? "no definido")
            .Replace("{{activity_level}}", request.ActivityLevel)
            .Replace("{{daily_calories}}", request.DailyCalories.ToString())
            .Replace("{{protein_g}}", request.ProteinGrams.ToString("F1"))
            .Replace("{{fat_g}}", request.FatGrams.ToString("F1"))
            .Replace("{{carbs_g}}", request.CarbsGrams.ToString("F1"))
            .Replace("{{allergies}}", string.Join(", ", request.Allergies))
            .Replace("{{disliked_ingredients}}", string.Join(", ", request.DislikedIngredients))
            .Replace("{{dietary_restrictions}}", string.Join(", ", request.DietaryRestrictions))
            .Replace("{{keto_experience}}", request.KetoExperienceLevel)
            .Replace("{{family_context}}", request.FamilyContext ?? "Sin contexto adicional")
            .Replace("{{previous_week_recipes}}", string.Join(", ", request.PreviousWeekRecipeCodes))
            .Replace("{{training_volume}}", request.ActivityLevel switch
            {
                "VeryActive" => "5-6 sesiones",
                "Active" => "3-4 sesiones",
                "Moderate" => "2-3 sesiones",
                _ => "1-2 sesiones"
            })
            + $"\n\n{GetOutputSchema()}";
    }

    private string BuildCorrectivePrompt(string template, GeneratePlanRequest request, string lastError)
    {
        return BuildPrompt(template, request) +
               $"\n\nCORRECCIÓN del intento anterior:\n{lastError}\n" +
               "Asegúrate de:\n" +
               "1. Retornar SOLO JSON válido\n" +
               "2. Respetar exactamente el schema de output\n" +
               "3. No exceder los límites de carbs\n" +
               "4. Incluir exactamente 7 días con 4 comidas cada uno (breakfast, lunch, dinner, snack)\n" +
               "5. Calcular macros que sumen 100%\n" +
               "6. No incluir ingredientes prohibidos para este modo";
    }

    private static string GetOutputSchema()
    {
        return """
        OUTPUT_SCHEMA (retorna EXACTAMENTE este JSON, sin markdown ni texto adicional):
        {
          "plan_metadata": {
            "mode_code": "string",
            "estimated_total_cost_mxn": 0.00,
            "estimated_cost_gourmet_baseline_mxn": 0.00,
            "savings_vs_gourmet_mxn": 0.00,
            "savings_vs_gourmet_percent": 0.00
          },
          "macros_target": { "daily_calories": 0, "daily_protein_g": 0.0, "daily_fat_g": 0.0, "daily_carbs_g": 0.0 },
          "days": [
            {
              "day_number": 1,
              "day_name": "Lunes",
              "meals": [
                {
                  "meal_type": "breakfast",
                  "recipe_name": "string",
                  "ingredients": [{ "ingredient_code": "", "name": "string", "amount_gr": 0, "unit_label": "g", "kcal": 0, "protein_g": 0, "fat_g": 0, "carbs_g": 0 }],
                  "instructions": "string",
                  "prep_time_min": 0,
                  "cook_time_min": 0,
                  "servings": 1,
                  "total_calories": 0,
                  "total_protein_g": 0,
                  "total_fat_g": 0,
                  "total_carbs_g": 0,
                  "estimated_cost_mxn": 0,
                  "tags": [],
                  "primary_store": ""
                }
              ],
              "day_totals": { "calories": 0, "protein_g": 0, "fat_g": 0, "carbs_g": 0, "estimated_cost_mxn": 0 }
            }
          ],
          "shopping_list_consolidated": [
            { "ingredient_code": "", "name": "string", "total_amount_gr": 0, "unit_label": "g", "store_category": "", "estimated_cost_mxn": 0, "category": "" }
          ]
        }
        """;
    }

    private static string GetPromptTemplate(string modeCode) => modeCode.ToLowerInvariant() switch
    {
        "economic" => PromptTemplates.EconomicV1,
        "pantry_basic" => PromptTemplates.PantryBasicV1,
        "simple_kitchen" => PromptTemplates.SimpleKitchenV1,
        "busy_parent" => PromptTemplates.BusyParentV1,
        "athletic" => PromptTemplates.AthleticV1,
        "gourmet" => PromptTemplates.GourmetV1,
        _ => PromptTemplates.PantryBasicV1,
    };

    private async Task<GeneratePlanResponse?> CheckCacheAsync(string requestHash, CancellationToken ct)
    {
        var cached = await _context.AiInteractions
            .Where(a => a.PromptHash == requestHash
                     && a.Success
                     && !a.CacheHit
                     && a.CreatedAt > DateTime.UtcNow.AddDays(-7))
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (cached?.ResponsePayload is null) return null;

        var cacheHit = new AiInteraction
        {
            Id = Guid.NewGuid(),
            UserId = null,
            InteractionType = AiInteractionType.PlanGeneration,
            PromptVersion = cached.PromptVersion,
            ModelUsed = cached.ModelUsed,
            PromptHash = requestHash,
            Success = true,
            CacheHit = true,
            CreatedAt = DateTime.UtcNow,
        };
        _context.AiInteractions.Add(cacheHit);
        await _context.SaveChangesAsync(ct);

        return JsonSerializer.Deserialize<GeneratePlanResponse>(cached.ResponsePayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private async Task LogInteractionAsync(
        GeneratePlanRequest request, string hash, string prompt, string response,
        int attempt, int inputTokens, int outputTokens, bool success,
        string? error, CancellationToken ct)
    {
        try
        {
            decimal estimatedCost = (inputTokens * InputPricePerToken) + (outputTokens * OutputPricePerToken);

            var interaction = new AiInteraction
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                InteractionType = AiInteractionType.PlanGeneration,
                PromptVersion = GetPromptVersion(request.BudgetModeCode),
                ModelUsed = _configuration["Gemini:PlanModel"] ?? "gemini-2.5-pro-preview-05-06",
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                EstimatedCostUsd = estimatedCost,
                DurationMs = attempt * 1000,
                Success = success,
                ErrorMessage = error,
                PromptHash = hash,
                RequestPayload = prompt.Length > 4000 ? prompt[..4000] : prompt,
                ResponsePayload = success ? (response.Length > 8000 ? response[..8000] : response) : null,
                CacheHit = false,
                CreatedAt = DateTime.UtcNow,
            };
            _context.AiInteractions.Add(interaction);
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registrando interacción AI en BD");
        }
    }

    private static string ComputeSha256Hash(string rawData)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private const string CircuitBreakerKey = "gemini_circuit_breaker";
    private const string ErrorCountKey = "gemini_error_count";
    private const string LastErrorTimeKey = "gemini_last_error_time";
    private const string TotalRequestsKey = "gemini_total_requests";

    private bool IsCircuitBreakerOpen()
    {
        if (_cache.Get<string>(CircuitBreakerKey) == "open")
        {
            var lastError = _cache.Get<DateTime>(LastErrorTimeKey);
            if (lastError != default && DateTime.UtcNow - lastError > TimeSpan.FromMinutes(5))
            {
                _cache.Set(CircuitBreakerKey, "half-open", TimeSpan.FromMinutes(1));
                return false;
            }
            return true;
        }
        return false;
    }

    private void RecordSuccess()
    {
        _cache.Set(CircuitBreakerKey, "closed", TimeSpan.FromMinutes(10));
        _cache.Set(ErrorCountKey, 0, TimeSpan.FromMinutes(10));
    }

    private void RecordFailure()
    {
        int errors = _cache.GetOrCreate(ErrorCountKey, e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return 0;
        }) + 1;

        _cache.Set(ErrorCountKey, errors, TimeSpan.FromMinutes(5));
        _cache.Set(LastErrorTimeKey, DateTime.UtcNow, TimeSpan.FromMinutes(5));

        int totalRequests = _cache.GetOrCreate(TotalRequestsKey, e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return 1;
        });

        _cache.Set(TotalRequestsKey, totalRequests + 1, TimeSpan.FromMinutes(5));

        double errorRate = (double)errors / totalRequests;
        if (errorRate > 0.30 || errors >= 3)
        {
            _cache.Set(CircuitBreakerKey, "open", TimeSpan.FromMinutes(5));
            _logger.LogWarning("Circuit breaker ABIERTO. Tasa de error: {Rate:P2}, Errores: {Errors}", errorRate, errors);
        }
    }

    private static string GetPromptVersion(string modeCode) => modeCode.ToLowerInvariant() switch
    {
        "economic" => PromptTemplates.Versions.Economic,
        "pantry_basic" => PromptTemplates.Versions.PantryBasic,
        "simple_kitchen" => PromptTemplates.Versions.SimpleKitchen,
        "busy_parent" => PromptTemplates.Versions.BusyParent,
        "athletic" => PromptTemplates.Versions.Athletic,
        "gourmet" => PromptTemplates.Versions.Gourmet,
        _ => PromptTemplates.Versions.PantryBasic,
    };
}
