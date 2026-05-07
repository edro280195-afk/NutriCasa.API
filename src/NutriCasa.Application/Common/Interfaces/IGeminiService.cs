namespace NutriCasa.Application.Common.Interfaces;

/// <summary>
/// Interfaz para interacciones con Gemini 2.5 Pro/Flash.
/// Fase 1: generación de planes, swaps, chat.
/// </summary>
public interface IGeminiService
{
    Task<string> GenerateWeeklyPlanAsync(string prompt, CancellationToken ct = default);
    Task<string> GenerateMealSwapAsync(string prompt, CancellationToken ct = default);
    Task<string> ChatAsync(string prompt, string? threadId = null, CancellationToken ct = default);
}
