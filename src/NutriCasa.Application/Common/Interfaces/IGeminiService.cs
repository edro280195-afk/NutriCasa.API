using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Common.Interfaces;

/// <summary>
/// Servicio de IA con Gemini 2.5 Pro/Flash para generación de planes y swaps.
/// Incluye cache, retry policy y circuit breaker.
/// </summary>
public interface IGeminiService
{
    Task<GeneratePlanResponse> GeneratePlanAsync(
        GeneratePlanRequest request,
        CancellationToken cancellationToken = default);

    Task<SwapMealResponse> SwapMealAsync(
        SwapMealRequest request,
        CancellationToken cancellationToken = default);

    // Fase 6 — Chat Q&A
    // Task<ChatResponse> ChatAsync(string prompt, string? threadId, CancellationToken ct = default);
}
