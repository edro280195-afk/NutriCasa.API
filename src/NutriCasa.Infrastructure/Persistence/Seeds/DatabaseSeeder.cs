using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NutriCasa.Infrastructure.Persistence;

namespace NutriCasa.Infrastructure.Persistence.Seeds;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            logger.LogInformation("Iniciando seed de datos...");

            await DisclaimerSeeder.SeedAsync(context);
            await SubscriptionPlanSeeder.SeedAsync(context);
            await FeatureFlagSeeder.SeedAsync(context);
            await SystemThresholdSeeder.SeedAsync(context);
            await ToxicWordSeeder.SeedAsync(context);
            await BudgetModeSeeder.SeedAsync(context);
            await StoreCategorySeeder.SeedAsync(context);
            await IngredientCatalogSeeder.SeedAsync(context);
            await IngredientSubstitutionSeeder.SeedAsync(context);

            logger.LogInformation("Seed de datos completado exitosamente.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error durante el seed de datos.");
            throw;
        }
    }
}
