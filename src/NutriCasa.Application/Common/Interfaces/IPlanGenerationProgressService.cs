using NutriCasa.Application.Features.Plans.Commands.GeneratePlan;

namespace NutriCasa.Application.Common.Interfaces;

/// <summary>
/// Envía notificaciones de progreso al cliente durante la generación de un plan
/// día a día via SignalR. Abstraído para que el Application layer no dependa de ASP.NET.
/// </summary>
public interface IPlanGenerationProgressService
{
    /// <summary>Notifica que comenzó la generación del plan completo.</summary>
    Task SendStartedAsync(Guid userId, Guid planId, int totalDays, CancellationToken ct = default);

    /// <summary>Notifica que un día quedó listo y envía sus comidas al cliente.</summary>
    Task SendDayReadyAsync(Guid userId, int dayNumber, string dayName, DayPlanDto day, int progressPercent, CancellationToken ct = default);

    /// <summary>Envía un mensaje motivacional durante el proceso.</summary>
    Task SendProgressMessageAsync(Guid userId, string emoji, string message, int progressPercent, CancellationToken ct = default);

    /// <summary>Notifica que el plan completo está listo.</summary>
    Task SendCompletedAsync(Guid userId, Guid planId, CancellationToken ct = default);

    /// <summary>Notifica un error parcial o total.</summary>
    Task SendErrorAsync(Guid userId, string message, int? dayNumber = null, CancellationToken ct = default);
}
