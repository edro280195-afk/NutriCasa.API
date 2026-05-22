using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NutriCasa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Health check extendido con información del entorno.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            Status = "healthy",
            Product = "NutriCasa",
            Service = "NutriCasa API",
            Version = "0.2.0",
            Phase = "Aplicacion funcional",
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Test endpoint to verify Resend email configuration.
    /// </summary>
    [HttpPost("test-email")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> TestEmail(
        [FromServices] NutriCasa.Application.Common.Interfaces.IEmailService emailService,
        [FromBody] TestEmailRequest request)
    {
        try
        {
            await emailService.SendEmailVerificationAsync(
                request.Email,
                request.Name ?? "Test",
                $"https://nutricasa.app/auth/verify-email?token=test-{Guid.NewGuid():N}",
                CancellationToken.None);
            return Ok(new { sent = true, message = "Correo enviado exitosamente" });
        }
        catch (Exception ex)
        {
            return Ok(new { sent = false, error = ex.Message, innerError = ex.InnerException?.Message });
        }
    }

    public class TestEmailRequest
    {
        public string Email { get; set; } = null!;
        public string? Name { get; set; }
    }

}
