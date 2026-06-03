using Microsoft.AspNetCore.SignalR;

namespace NutriCasa.Infrastructure.Services;

/// <summary>
/// Hub SignalR para notificar al cliente el progreso de generación de su plan
/// keto día a día. Cada usuario se une a su propio grupo (userId) al conectar.
/// Endpoint: /hubs/plan-generation
/// </summary>
public class PlanGenerationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userIdStr = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdStr))
        {
            // Cada usuario tiene su propio grupo personal para recibir sus propios eventos.
            await Groups.AddToGroupAsync(Context.ConnectionId, $"plan-gen-{userIdStr}");
        }
        await base.OnConnectedAsync();
    }
}
