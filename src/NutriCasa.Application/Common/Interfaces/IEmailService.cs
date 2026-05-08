namespace NutriCasa.Application.Common.Interfaces;

/// <summary>
/// Servicio de correo electrónico con Resend.
/// Todos los emails usan el design system de NutriCasa (fondo #F8F4EC, verde #0F3D2E, botón #5BC096).
/// </summary>
public interface IEmailService
{
    Task SendEmailVerificationAsync(
        string toEmail,
        string toName,
        string verificationLink,
        CancellationToken ct = default);

    Task SendPasswordResetAsync(
        string toEmail,
        string toName,
        string resetLink,
        CancellationToken ct = default);

    Task SendLoginAlertAsync(
        string toEmail,
        string toName,
        string ipAddress,
        DateTime timestamp,
        CancellationToken ct = default);

    Task SendWelcomeAsync(
        string toEmail,
        string toName,
        CancellationToken ct = default);
}
