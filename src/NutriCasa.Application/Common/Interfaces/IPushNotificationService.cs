namespace NutriCasa.Application.Common.Interfaces;

public interface IPushNotificationService
{
    Task SendToUserAsync(Guid userId, string title, string body, string? deepLink = null, string? type = null, CancellationToken ct = default);
    string GetVapidPublicKey();
}
