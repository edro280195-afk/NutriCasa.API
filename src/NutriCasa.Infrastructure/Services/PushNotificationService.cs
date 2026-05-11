using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;
using NutriCasa.Infrastructure.Persistence;
using WebPush;

namespace NutriCasa.Infrastructure.Services;

public class PushNotificationService : IPushNotificationService, IDisposable
{
    private readonly WebPushClient _client;
    private readonly string _vapidPublicKey;
    private readonly string _vapidPrivateKey;
    private readonly string _vapidSubject;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PushNotificationService> _logger;
    private bool _disposed;

    public PushNotificationService(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<PushNotificationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _client = new WebPushClient();

        _vapidSubject = configuration["Vapid:Subject"] ?? "mailto:admin@nutricasa.app";
        _vapidPublicKey = configuration["Vapid:PublicKey"] ?? "";
        _vapidPrivateKey = configuration["Vapid:PrivateKey"] ?? "";

        if (string.IsNullOrEmpty(_vapidPublicKey) || string.IsNullOrEmpty(_vapidPrivateKey))
        {
            var keys = VapidHelper.GenerateVapidKeys();
            _vapidPublicKey = keys.PublicKey;
            _vapidPrivateKey = keys.PrivateKey;
            _logger.LogWarning(
                "VAPID keys no configuradas en appsettings. Generadas automáticamente.\n" +
                "Pública: {PublicKey}\nPrivada: {PrivateKey}",
                _vapidPublicKey, _vapidPrivateKey);
        }
    }

    public string GetVapidPublicKey() => _vapidPublicKey;

    public async Task SendToUserAsync(Guid userId, string title, string body, string? deepLink = null, string? type = null, CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var subs = await context.PushSubscriptions
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync(ct);

        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            notification = new
            {
                title,
                body,
                icon = "/favicon.ico",
                badge = "/favicon.ico",
                data = new
                {
                    deepLink,
                    type = type ?? "general",
                },
            },
        });

        foreach (var sub in subs)
        {
            try
            {
                var pushSub = new WebPush.PushSubscription(sub.Endpoint, sub.P256dhKey, sub.AuthKey);
                await _client.SendNotificationAsync(pushSub, payload, new VapidDetails(_vapidSubject, _vapidPublicKey, _vapidPrivateKey), ct);

                sub.LastUsedAt = DateTime.UtcNow;
            }
            catch (WebPushException ex)
            {
                _logger.LogError(ex, "Error sending push to {Endpoint}", sub.Endpoint);
                if (ex.StatusCode == System.Net.HttpStatusCode.Gone || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    sub.IsActive = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending push to {Endpoint}", sub.Endpoint);
            }
        }

        var notification = new Notification
        {
            UserId = userId,
            Type = type ?? "general",
            Priority = NotificationPriority.P3,
            Title = title,
            Body = body,
            DeepLink = deepLink,
            DeliveryChannels = ["push"],
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        context.Notifications.Add(notification);

        await context.SaveChangesAsync(ct);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _client?.Dispose();
            _disposed = true;
        }
    }
}
