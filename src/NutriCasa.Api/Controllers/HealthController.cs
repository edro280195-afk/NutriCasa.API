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
            Version = "0.1.0",
            Phase = "Fase 0 — Foundation",
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Test endpoint to verify Resend email configuration.
    /// </summary>
    [HttpPost("test-email")]
    public async Task<IActionResult> TestEmail(
        [FromServices] NutriCasa.Application.Common.Interfaces.IEmailService emailService,
        [FromBody] TestEmailRequest request)
    {
        try
        {
            await emailService.SendEmailVerificationAsync(
                request.Email,
                request.Name ?? "Test",
                "https://nutricasa.app/verify-email?token=test123",
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

    /// <summary>
    /// Temporary endpoint for Fase 0 to truncate tables and force re-seed.
    /// </summary>
    [HttpDelete("truncate")]
    public async Task<IActionResult> TruncateTables([FromServices] NutriCasa.Application.Common.Interfaces.IApplicationDbContext context)
    {
        var db = (Microsoft.EntityFrameworkCore.DbContext)context;
        await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync(db.Database, "TRUNCATE TABLE feature_flags CASCADE;");
        await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync(db.Database, "TRUNCATE TABLE ingredient_catalog CASCADE;");
        return Ok("Tablas truncadas exitosamente.");
    }
}
