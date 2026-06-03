using Microsoft.AspNetCore.SignalR;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Features.Plans.Commands.GeneratePlan;

namespace NutriCasa.Infrastructure.Services;

/// <summary>
/// Implementación de IPlanGenerationProgressService que emite eventos SignalR
/// al grupo personal del usuario (plan-gen-{userId}) en PlanGenerationHub.
/// </summary>
public class SignalRPlanProgressService : IPlanGenerationProgressService
{
    private readonly IHubContext<PlanGenerationHub> _hubContext;

    public SignalRPlanProgressService(IHubContext<PlanGenerationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task SendStartedAsync(Guid userId, Guid planId, int totalDays, CancellationToken ct = default)
        => _hubContext.Clients
            .Group($"plan-gen-{userId}")
            .SendAsync("plan:started", new
            {
                planId,
                totalDays,
                message = "🚀 Comenzando a preparar tu plan keto...",
                progress = 0
            }, ct);

    public Task SendDayReadyAsync(Guid userId, int dayNumber, string dayName, DayPlanDto day, int progressPercent, CancellationToken ct = default)
        => _hubContext.Clients
            .Group($"plan-gen-{userId}")
            .SendAsync("plan:day_ready", new
            {
                dayNumber,
                dayName,
                day,
                progress = progressPercent
            }, ct);

    public Task SendProgressMessageAsync(Guid userId, string emoji, string message, int progressPercent, CancellationToken ct = default)
        => _hubContext.Clients
            .Group($"plan-gen-{userId}")
            .SendAsync("plan:progress", new
            {
                emoji,
                message,
                progress = progressPercent
            }, ct);

    public Task SendCompletedAsync(Guid userId, Guid planId, CancellationToken ct = default)
        => _hubContext.Clients
            .Group($"plan-gen-{userId}")
            .SendAsync("plan:completed", new
            {
                planId,
                message = "✅ ¡Tu plan keto de la semana está listo!",
                progress = 100
            }, ct);

    public Task SendErrorAsync(Guid userId, string message, int? dayNumber = null, CancellationToken ct = default)
        => _hubContext.Clients
            .Group($"plan-gen-{userId}")
            .SendAsync("plan:error", new
            {
                message,
                dayNumber
            }, ct);
}
