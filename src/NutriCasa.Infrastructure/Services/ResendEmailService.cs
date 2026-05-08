using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NutriCasa.Application.Common.Interfaces;

namespace NutriCasa.Infrastructure.Services;

public class ResendEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public ResendEmailService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Resend:ApiKey"] ?? throw new InvalidOperationException("Resend:ApiKey no configurada.");
        _fromEmail = configuration["Resend:FromEmail"] ?? "hola@nutricasa.app";
        _fromName = configuration["Resend:FromName"] ?? "NutriCasa";

        _httpClient.BaseAddress = new Uri("https://api.resend.com/");
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {_apiKey}");
    }

    public async Task SendEmailVerificationAsync(string toEmail, string toName, string verificationLink, CancellationToken ct = default)
    {
        var body = BuildEmailHtml(
            "Activa tu cuenta en NutriCasa 🌿",
            $"<p>Hola <strong>{toName}</strong>,</p>" +
            $"<p>Gracias por registrarte en NutriCasa. Para activar tu cuenta, haz clic en el siguiente botón:</p>" +
            $"<div style=\"text-align:center;margin:30px 0;\">" +
            $"<a href=\"{verificationLink}\" style=\"background-color:#5BC096;color:#0A2A20;padding:14px 32px;text-decoration:none;border-radius:8px;font-weight:bold;display:inline-block;\">Verificar mi cuenta</a>" +
            $"</div>" +
            $"<p style=\"color:#8A9590;font-size:12px;\">Si no puedes hacer clic, copia este enlace: {verificationLink}</p>"
        );

        await SendEmailAsync(toEmail, "Activa tu cuenta en NutriCasa 🌿", body, ct);
    }

    public async Task SendPasswordResetAsync(string toEmail, string toName, string resetLink, CancellationToken ct = default)
    {
        var body = BuildEmailHtml(
            "Restablece tu contraseña en NutriCasa",
            $"<p>Hola <strong>{toName}</strong>,</p>" +
            $"<p>Recibimos una solicitud para restablecer tu contraseña. Haz clic en el siguiente botón para crear una nueva:</p>" +
            $"<div style=\"text-align:center;margin:30px 0;\">" +
            $"<a href=\"{resetLink}\" style=\"background-color:#5BC096;color:#0A2A20;padding:14px 32px;text-decoration:none;border-radius:8px;font-weight:bold;display:inline-block;\">Restablecer contraseña</a>" +
            $"</div>" +
            $"<p style=\"color:#8A9590;font-size:12px;\">Si no solicitaste esto, ignora este mensaje. El enlace expira en 1 hora.</p>"
        );

        await SendEmailAsync(toEmail, "Restablece tu contraseña en NutriCasa", body, ct);
    }

    public async Task SendLoginAlertAsync(string toEmail, string toName, string ipAddress, DateTime timestamp, CancellationToken ct = default)
    {
        var body = BuildEmailHtml(
            "Alerta de inicio de sesión",
            $"<p>Hola <strong>{toName}</strong>,</p>" +
            $"<p>Se detectó un inicio de sesión en tu cuenta de NutriCasa:</p>" +
            $"<p style=\"background-color:#FBE6DD;padding:12px;border-radius:8px;\">" +
            $"📍 IP: {ipAddress}<br/>" +
            $"🕐 Fecha: {timestamp:dd/MM/yyyy HH:mm} UTC</p>" +
            $"<p>Si no fuiste tú, cambia tu contraseña inmediatamente.</p>"
        );

        await SendEmailAsync(toEmail, "Alerta de inicio de sesión — NutriCasa", body, ct);
    }

    public async Task SendWelcomeAsync(string toEmail, string toName, CancellationToken ct = default)
    {
        var body = BuildEmailHtml(
            "¡Bienvenido a NutriCasa! 🌿",
            $"<p>Hola <strong>{toName}</strong>,</p>" +
            $"<p>¡Bienvenido a NutriCasa! Estamos emocionados de acompañarte en tu viaje de nutrición cetogénica.</p>" +
            $"<p>Esto es lo que puedes esperar:</p>" +
            $"<ul>" +
            $"<li>Planes semanales adaptados a tu cuerpo y presupuesto</li>" +
            $"<li>Soporte para toda tu familia</li>" +
            $"<li>Recetas deliciosas y fáciles</li>" +
            $"</ul>" +
            $"<p>Si tienes dudas, responde a este correo.</p>"
        );

        await SendEmailAsync(toEmail, "¡Bienvenido a NutriCasa! 🌿", body, ct);
    }

    private async Task SendEmailAsync(string to, string subject, string html, CancellationToken ct)
    {
        var payload = new
        {
            from = $"{_fromName} <{_fromEmail}>",
            to = new[] { to },
            subject,
            html,
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("email", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Error enviando email: {response.StatusCode} - {error}");
        }
    }

    private static string BuildEmailHtml(string title, string bodyContent)
    {
        return $"<!DOCTYPE html><html lang=\"es\"><head><meta charset=\"UTF-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>{title}</title></head>" +
               $"<body style=\"margin:0;padding:0;background-color:#F8F4EC;font-family:system-ui,-apple-system,sans-serif;\">" +
               $"<div style=\"max-width:600px;margin:0 auto;padding:32px 24px;\">" +
               $"<div style=\"background-color:#FFFFFF;border-radius:12px;padding:32px;box-shadow:0 2px 8px rgba(0,0,0,0.05);\">" +
               $"<h1 style=\"color:#0F3D2E;font-size:24px;margin:0 0 24px;\">NutriCasa</h1>" +
               $"{bodyContent}" +
               $"</div>" +
               $"<p style=\"text-align:center;color:#8A9590;font-size:12px;margin-top:24px;\">" +
               $"NutriCasa · Nutrición adaptada a tu vida · nutricasa.app</p>" +
               $"</div></body></html>";
    }
}
