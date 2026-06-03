using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    // Modelo de Gemini. El preview con fecha (gemini-2.5-pro-preview-05-06) fue
    // retirado por Google (404). Se usa gemini-2.5-flash: mucho más rápido que pro,
    // evita que el cliente cancele la petición por timeout. Sobreescribible vía
    // "Gemini:PlanModel" (gemini-2.5-pro requiere billing y es más lento).
    private const string DefaultModel = "gemini-2.5-flash";

    // Presupuesto de salida por defecto. Un plan de 7 días × 4 comidas + lista de
    // compras es un JSON grande; con valores bajos la respuesta se trunca y queda
    // inválida. Sobreescribible vía "Gemini:MaxOutputTokens".
    private const int DefaultMaxOutputTokens = 65536;

    // Opciones tolerantes: snake_case + acepta decimales/strings donde el DTO espera
    // enteros (Gemini a veces devuelve 412.5 o "412" para total_calories, etc.).
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters =
        {
            new FlexibleIntConverter(),
            new FlexibleDecimalConverter(),
        },
    };

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

        var cachedResponse = request.ForceRegenerate ? null : await CheckCacheAsync(requestHash, cancellationToken);
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

    public async Task<SwapMealResponse> SwapMealAsync(SwapMealRequest request, CancellationToken cancellationToken = default)
    {
        if (IsCircuitBreakerOpen())
            throw new InvalidOperationException("Circuit breaker abierto. Usar fallback.");

        string requestHash = ComputeSha256Hash(JsonSerializer.Serialize(request));

        var cachedResponse = await CheckSwapCacheAsync(requestHash, cancellationToken);
        if (cachedResponse is not null)
        {
            _logger.LogInformation("Cache hit para swap del usuario {UserId}", request.UserId);
            return cachedResponse;
        }

        string prompt = BuildSwapPrompt(request);

        string? lastError = null;
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var response = await CallGeminiForSwapAsync(prompt, cancellationToken);

                await LogSwapInteractionAsync(request, requestHash, prompt, response.raw, attempt, response.inputTokens, response.outputTokens, true, null, cancellationToken);
                _logger.LogInformation("Swap meal generado para usuario {UserId} (intento {Attempt})", request.UserId, attempt);

                RecordSuccess();

                return response.swap;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                _logger.LogWarning("Intento {Attempt} falló para swap Gemini: {Error}", attempt, ex.Message);

                if (attempt < 3)
                {
                    prompt = BuildSwapCorrectivePrompt(request, lastError);
                    await Task.Delay(1000 * attempt, cancellationToken);
                }
            }
        }

        await LogSwapInteractionAsync(request, requestHash, prompt, lastError!, 3, 0, 0, false, lastError, cancellationToken);
        RecordFailure();
        throw new InvalidOperationException($"Gemini swap falló tras 3 intentos: {lastError}");
    }

    private async Task<(GeneratePlanResponse plan, string raw, int inputTokens, int outputTokens)> CallGeminiAsync(string prompt, bool isOverride, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("Gemini");
        string model = _configuration["Gemini:PlanModel"] ?? DefaultModel;

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
                maxOutputTokens = _configuration.GetValue("Gemini:MaxOutputTokens", DefaultMaxOutputTokens),
                responseMimeType = "application/json"
            }
        };

        var response = await client.PostAsJsonAsync(
            $"./{model}:generateContent",
            requestBody, ct);

        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(jsonResponse);
        var candidate = doc.RootElement.GetProperty("candidates")[0];

        // Validar finishReason antes de intentar parsear
        var finishReason = candidate.TryGetProperty("finishReason", out var fr) ? fr.GetString() : null;
        if (finishReason == "MAX_TOKENS")
            throw new InvalidOperationException("Gemini truncó la respuesta por límite de tokens. La respuesta JSON está incompleta.");

        var text = candidate
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "{}";

        // Extraer tokens reales de usageMetadata si están disponibles
        int inputTokens = prompt.Length / 4;
        int outputTokens = text.Length / 4;
        if (doc.RootElement.TryGetProperty("usageMetadata", out var usage))
        {
            if (usage.TryGetProperty("promptTokenCount", out var ptc))
                inputTokens = ptc.GetInt32();
            if (usage.TryGetProperty("candidatesTokenCount", out var ctc))
                outputTokens = ctc.GetInt32();
        }

        var plan = JsonSerializer.Deserialize<GeneratePlanResponse>(text, JsonOptions);
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
            + (!string.IsNullOrEmpty(request.RandomSeed) ? $"\n\nSEED DE VARIEDAD: {request.RandomSeed} (asegúrate de generar platillos completamente diferentes a intentos anteriores)." : "")
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
               "6. No incluir ingredientes prohibidos para este modo\n" +
               "7. Los SNACKS deben ser bocadillos simples (max 3 ingredientes, prep_time_min <= 5, cook_time_min = 0). NO platillos completos.";
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

        NOTA IMPORTANTE SOBRE SNACKS:
        Para meal_type "snack": bocadillo MUY LIGERO, máximo 3 ingredientes, prep_time_min <= 5, cook_time_min = 0.
        PROHIBIDO usar carnes, aves, pescados o mariscos (como atún o ceviche) en los snacks. 
        Instrucciones de 1 oración. NO es un platillo completo.
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

    // ─────────────────────────────────────────────────────────────────────────
    // Generación por Día (reemplaza el prompt monolítico de 7 días)
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<DayPlanResponse> GenerateDayPlanAsync(GenerateDayRequest request, CancellationToken cancellationToken = default)
    {
        if (IsCircuitBreakerOpen())
            throw new InvalidOperationException("Circuit breaker abierto. Usar fallback.");

        string prompt = BuildDayPrompt(request);
        string? lastError = null;

        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var response = await CallGeminiForDayAsync(prompt, cancellationToken);
                _logger.LogInformation("Gemini día {Day} generado para usuario {UserId} (intento {Attempt})",
                    request.DayNumber, request.UserId, attempt);
                RecordSuccess();
                return response;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                _logger.LogWarning("Intento {Attempt} para día {Day} falló: {Error}", attempt, request.DayNumber, ex.Message);
                if (attempt < 3)
                    await Task.Delay(800 * attempt, cancellationToken);
            }
        }

        RecordFailure();
        throw new InvalidOperationException($"Gemini día {request.DayNumber} falló tras 3 intentos: {lastError}");
    }

    private async Task<DayPlanResponse> CallGeminiForDayAsync(string prompt, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("Gemini");
        string model = _configuration["Gemini:PlanModel"] ?? DefaultModel;

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new
            {
                temperature = 0.85,   // ligeramente más alto que el plan completo para forzar variedad
                maxOutputTokens = 8192,
                responseMimeType = "application/json"
            }
        };

        var response = await client.PostAsJsonAsync($"./{model}:generateContent", requestBody, ct);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(jsonResponse);
        var candidate = doc.RootElement.GetProperty("candidates")[0];

        var finishReason = candidate.TryGetProperty("finishReason", out var fr) ? fr.GetString() : null;
        if (finishReason == "MAX_TOKENS")
        {
            string textPart = "";
            try
            {
                textPart = candidate.GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
            }
            catch {}
            string startSnippet = textPart.Substring(0, Math.Min(textPart.Length, 1500));
            string endSnippet = textPart.Length > 1500 ? textPart.Substring(textPart.Length - 500) : "";
            throw new InvalidOperationException($"Gemini truncó la respuesta del día por límite de tokens. Longitud total: {textPart.Length}. Inicio: {startSnippet} ... Fin: {endSnippet}");
        }

        var text = candidate
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "{}";

        var dayPlan = JsonSerializer.Deserialize<DayPlan>(text, JsonOptions);
        if (dayPlan is null)
            throw new InvalidOperationException($"No se pudo deserializar el día desde Gemini.");

        return new DayPlanResponse { Day = dayPlan, RawJson = text };
    }

    private string BuildDayPrompt(GenerateDayRequest request)
    {
        string baseTemplate = GetPromptTemplate(request.BudgetModeCode);

        // Reemplazar placeholders del template base con la información del usuario
        string templateProcessed = baseTemplate
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
            });

        string alreadyUsed = request.AlreadyUsedRecipeNames.Length > 0
            ? $"Ya generados esta semana (NO repetir): {string.Join(", ", request.AlreadyUsedRecipeNames)}."
            : "Primer día de la semana.";

        string previousWeek = request.PreviousWeekRecipeCodes.Length > 0
            ? $"Semana pasada: {string.Join(", ", request.PreviousWeekRecipeCodes.Take(10))}."
            : "Sin historial previo.";

        string seed = !string.IsNullOrEmpty(request.RandomSeed)
            ? $"\nSEED DE VARIEDAD: {request.RandomSeed} — genera opciones únicas y creativas."
            : "";

        return $"""
            {templateProcessed}

            ================================================================================
            INSTRUCCIÓN DE GENERACIÓN ESPECÍFICA (DÍA ÚNICO):
            ================================================================================
            Estás generando ÚNICAMENTE el día: {request.DayName} (día {request.DayNumber} de 7).
            PROHIBIDO generar otros días. Devuelve SOLO el objeto JSON para {request.DayName} siguiendo el esquema del final.

            Detalles de control del día:
            {alreadyUsed}
            {previousWeek}
            {seed}

            {GetDayOutputSchema(request.DayNumber, request.DayName)}
            """;
    }

    private static string GetDayOutputSchema(int dayNumber, string dayName) => $$"""
        OUTPUT_SCHEMA — Devuelve SOLO este JSON para el día {{dayName}}, sin texto adicional:
        {
          "day_number": {{dayNumber}},
          "day_name": "{{dayName}}",
          "meals": [
            {
              "meal_type": "breakfast",
              "recipe_name": "string",
              "ingredients": [{"ingredient_code": "", "name": "string", "amount_gr": 0, "unit_label": "g", "kcal": 0, "protein_g": 0, "fat_g": 0, "carbs_g": 0}],
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
          "day_totals": {"calories": 0, "protein_g": 0, "fat_g": 0, "carbs_g": 0, "estimated_cost_mxn": 0}
        }

        REGLAS SNACK: bocadillo MUY ligero (máx 3 ingredientes, prep ≤5min, cook=0).
        PROHIBIDO en snacks: carnes, aves, pescados, mariscos, ceviche.
        Genera exactamente 4 comidas: breakfast, lunch, dinner, snack.
        """;

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

        return JsonSerializer.Deserialize<GeneratePlanResponse>(cached.ResponsePayload, JsonOptions);
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
                ModelUsed = _configuration["Gemini:PlanModel"] ?? DefaultModel,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                EstimatedCostUsd = estimatedCost,
                DurationMs = attempt * 1000,
                Success = success,
                ErrorMessage = error,
                PromptHash = hash,
                RequestPayload = JsonSerializer.Serialize(new { prompt = prompt.Length > 4000 ? prompt[..4000] : prompt }),
                ResponsePayload = success ? response : null,
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

    private async Task<SwapMealResponse?> CheckSwapCacheAsync(string requestHash, CancellationToken ct)
    {
        var cached = await _context.AiInteractions
            .Where(a => a.PromptHash == requestHash
                     && a.InteractionType == AiInteractionType.MealSwap
                     && a.Success
                     && !a.CacheHit
                     && a.CreatedAt > DateTime.UtcNow.AddDays(-1))
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (cached?.ResponsePayload is null) return null;

        var cacheHit = new AiInteraction
        {
            Id = Guid.NewGuid(),
            UserId = null,
            InteractionType = AiInteractionType.MealSwap,
            PromptVersion = cached.PromptVersion,
            ModelUsed = cached.ModelUsed,
            PromptHash = requestHash,
            Success = true,
            CacheHit = true,
            CreatedAt = DateTime.UtcNow,
        };
        _context.AiInteractions.Add(cacheHit);
        await _context.SaveChangesAsync(ct);

        return JsonSerializer.Deserialize<SwapMealResponse>(cached.ResponsePayload, JsonOptions);
    }

    private async Task<(SwapMealResponse swap, string raw, int inputTokens, int outputTokens)> CallGeminiForSwapAsync(string prompt, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("Gemini");
        string model = _configuration["Gemini:PlanModel"] ?? DefaultModel;

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
                temperature = 0.5,
                maxOutputTokens = 8192,
                responseMimeType = "application/json"
            }
        };

        var response = await client.PostAsJsonAsync(
            $"./{model}:generateContent",
            requestBody, ct);

        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(jsonResponse);
        var candidate = doc.RootElement.GetProperty("candidates")[0];

        // Validar finishReason antes de intentar parsear
        var finishReason = candidate.TryGetProperty("finishReason", out var fr) ? fr.GetString() : null;
        if (finishReason == "MAX_TOKENS")
            throw new InvalidOperationException("Gemini truncó la respuesta swap por límite de tokens.");

        var text = candidate
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "{}";

        // Extraer tokens reales de usageMetadata si están disponibles
        int inputTokens = prompt.Length / 4;
        int outputTokens = text.Length / 4;
        if (doc.RootElement.TryGetProperty("usageMetadata", out var usage))
        {
            if (usage.TryGetProperty("promptTokenCount", out var ptc))
                inputTokens = ptc.GetInt32();
            if (usage.TryGetProperty("candidatesTokenCount", out var ctc))
                outputTokens = ctc.GetInt32();
        }

        var swap = JsonSerializer.Deserialize<SwapMealResponse>(text, JsonOptions);
        if (swap is null)
            throw new InvalidOperationException("La respuesta de Gemini swap no pudo ser deserializada.");

        swap = swap with { RawJson = text };

        return (swap, text, inputTokens, outputTokens);
    }

    private string BuildSwapPrompt(SwapMealRequest request)
    {
        string template = PromptTemplates.SwapMealV1;

        string budgetCostLimit = request.BudgetModeCode.ToLowerInvariant() switch
        {
            "economic" => "$35 MXN",
            "pantry_basic" => "$50 MXN",
            "simple_kitchen" => "$50 MXN",
            "busy_parent" => "$60 MXN",
            "athletic" => "$80 MXN",
            "gourmet" => "$150 MXN",
            _ => "$50 MXN"
        };

        return template
            .Replace("{{budget_mode}}", request.BudgetModeCode)
            .Replace("{{current_recipe}}", request.CurrentRecipeName)
            .Replace("{{meal_type}}", request.MealType)
            .Replace("{{swap_reason}}", request.SwapReason ?? "El usuario quiere variar el menú")
            .Replace("{{daily_calories}}", request.DailyCaloriesTarget.ToString())
            .Replace("{{protein_g}}", request.ProteinTarget.ToString("F1"))
            .Replace("{{fat_g}}", request.FatTarget.ToString("F1"))
            .Replace("{{carbs_g}}", request.CarbsTarget.ToString("F1"))
            .Replace("{{allergies}}", string.Join(", ", request.Allergies))
            .Replace("{{disliked_ingredients}}", string.Join(", ", request.DislikedIngredients))
            + $"\n\nCosto máximo por comida: {budgetCostLimit}.\n"
            + GetSwapOutputSchema();
    }

    private static string GetSwapOutputSchema()
    {
        return """
        OUTPUT_SCHEMA (retorna EXACTAMENTE este JSON, sin markdown ni texto adicional):
        {
          "new_meal": {
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
        }
        """;
    }

    private string BuildSwapCorrectivePrompt(SwapMealRequest request, string lastError)
    {
        return BuildSwapPrompt(request) +
               $"\n\nCORRECCIÓN del intento anterior:\n{lastError}\n" +
               "Asegúrate de:\n" +
               "1. Retornar SOLO JSON válido\n" +
               "2. Respetar exactamente el schema de output\n" +
               "3. Los macros no deben desviarse más del 10% del objetivo\n" +
               "4. No incluir ingredientes alergénicos\n" +
               "5. Respetar el costo máximo por comida\n" +
               "6. La receta debe ser keto-friendly (< 20g carbs netos por porción)";
    }

    private async Task LogSwapInteractionAsync(
        SwapMealRequest request, string hash, string prompt, string response,
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
                InteractionType = AiInteractionType.MealSwap,
                PromptVersion = PromptTemplates.Versions.SwapMeal,
                ModelUsed = _configuration["Gemini:PlanModel"] ?? DefaultModel,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                EstimatedCostUsd = estimatedCost,
                DurationMs = attempt * 1000,
                Success = success,
                ErrorMessage = error,
                PromptHash = hash,
                RequestPayload = JsonSerializer.Serialize(new { prompt = prompt.Length > 4000 ? prompt[..4000] : prompt }),
                ResponsePayload = success ? response : null,
                CacheHit = false,
                CreatedAt = DateTime.UtcNow,
            };
            _context.AiInteractions.Add(interaction);
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registrando swap AI en BD");
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

        if (errors >= 3)
        {
            _cache.Set(CircuitBreakerKey, "open", TimeSpan.FromMinutes(5));
            _logger.LogWarning("Circuit breaker ABIERTO tras {Errors} errores consecutivos.", errors);
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

// Convierte enteros aunque Gemini devuelva decimales (412.5) o strings ("412").
internal sealed class FlexibleIntConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                if (reader.TryGetInt32(out int i)) return i;
                if (reader.TryGetDouble(out double dbl)) return (int)Math.Round(dbl, MidpointRounding.AwayFromZero);
                return 0;
            case JsonTokenType.String:
                string? s = reader.GetString();
                if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double sd))
                    return (int)Math.Round(sd, MidpointRounding.AwayFromZero);
                return 0;
            case JsonTokenType.Null:
                return 0;
            default:
                reader.Skip();
                return 0;
        }
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}

// Convierte decimales aunque Gemini devuelva el número como string ("412.5").
internal sealed class FlexibleDecimalConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                if (reader.TryGetDecimal(out decimal d)) return d;
                if (reader.TryGetDouble(out double dbl)) return (decimal)dbl;
                return 0m;
            case JsonTokenType.String:
                string? s = reader.GetString();
                if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal sd))
                    return sd;
                return 0m;
            case JsonTokenType.Null:
                return 0m;
            default:
                reader.Skip();
                return 0m;
        }
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}
