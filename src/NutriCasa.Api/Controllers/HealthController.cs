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
