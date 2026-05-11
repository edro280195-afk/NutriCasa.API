using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriCasa.Infrastructure.Persistence;
using NutriCasa.Infrastructure.Persistence.Seeds;

namespace NutriCasa.Api.Controllers;

[Authorize]
public class AdminController : BaseApiController
{
    [HttpPost("seed-curated-recipes")]
    public async Task<IActionResult> SeedCuratedRecipes(
        [FromServices] ApplicationDbContext context,
        CancellationToken ct)
    {
        await CuratedRecipeSeeder.SeedAsync(context);
        return Ok(new { message = "Recetas curadas sembradas exitosamente" });
    }
}
