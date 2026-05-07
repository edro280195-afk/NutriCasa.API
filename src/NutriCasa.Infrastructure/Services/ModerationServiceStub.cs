using NutriCasa.Application.Common.Interfaces;

namespace NutriCasa.Infrastructure.Services;

/// <summary>
/// Pendiente de Fase 4 — Filtro híbrido de toxicidad.
/// </summary>
public class ModerationServiceStub : IModerationService
{
    public Task<(bool IsClean, string? Reason, string? Severity)> ModerateTextAsync(string text, CancellationToken ct = default)
        => throw new NotImplementedException("Pendiente de Fase 4 — Filtro híbrido de toxicidad");
}
