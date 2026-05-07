using NutriCasa.Application.Common.Interfaces;

namespace NutriCasa.Infrastructure.Services;

/// <summary>
/// Pendiente de Fase 1 — Validador de respuestas IA.
/// </summary>
public class PlanValidatorStub : IPlanValidator
{
    public Task<bool> ValidateAsync(string planJson, string modeCode, CancellationToken ct = default)
        => throw new NotImplementedException("Pendiente de Fase 1 — Validador de respuestas IA");
}
