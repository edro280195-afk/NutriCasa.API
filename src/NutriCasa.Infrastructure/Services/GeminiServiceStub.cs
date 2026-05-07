using NutriCasa.Application.Common.Interfaces;

namespace NutriCasa.Infrastructure.Services;

/// <summary>
/// Pendiente de Fase 1 — Integración con Gemini 2.5 Pro/Flash.
/// </summary>
public class GeminiServiceStub : IGeminiService
{
    public Task<string> GenerateWeeklyPlanAsync(string prompt, CancellationToken ct = default)
        => throw new NotImplementedException("Pendiente de Fase 1 — Integración con Gemini 2.5 Pro/Flash");

    public Task<string> GenerateMealSwapAsync(string prompt, CancellationToken ct = default)
        => throw new NotImplementedException("Pendiente de Fase 1 — Integración con Gemini 2.5 Pro/Flash");

    public Task<string> ChatAsync(string prompt, string? threadId = null, CancellationToken ct = default)
        => throw new NotImplementedException("Pendiente de Fase 1 — Integración con Gemini 2.5 Pro/Flash");
}
