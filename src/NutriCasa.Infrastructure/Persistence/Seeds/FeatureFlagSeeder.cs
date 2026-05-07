using Microsoft.EntityFrameworkCore;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Infrastructure.Persistence.Seeds;

public static class FeatureFlagSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.FeatureFlags.AnyAsync()) return;

        var flags = new List<FeatureFlag>
        {
            new() { Code = "ai_chat_v1", Description = "Chat Q&A con coach IA (Fase 6)", IsEnabled = false, RolloutPercent = 0 },
            new() { Code = "photo_food_recognition", Description = "Reconocimiento de comida por foto (Fase 9)", IsEnabled = false, RolloutPercent = 0 },
            new() { Code = "wearable_integration", Description = "Integración con Google Fit / Apple Health (Fase 9)", IsEnabled = false, RolloutPercent = 0 },
            new() { Code = "export_pdf", Description = "Exportar plan y reportes a PDF", IsEnabled = false, RolloutPercent = 0 },
            new() { Code = "shopping_list_whatsapp", Description = "Compartir lista de compras vía WhatsApp", IsEnabled = false, RolloutPercent = 0 },
            new() { Code = "challenge_betting", Description = "Apuestas dentro de retos grupales", IsEnabled = false, RolloutPercent = 0 },
            new() { Code = "minor_users_v2", Description = "Soporte de menores 16-17 con tutor (V2)", IsEnabled = false, RolloutPercent = 0 },
            new() { Code = "post_moderation_gemini", Description = "Capa 2 de moderación con Gemini en posts > 200 chars", IsEnabled = false, RolloutPercent = 0 },
            new() { Code = "account_deletion_self_service", Description = "Permitir borrado de cuenta auto-servicio", IsEnabled = true, RolloutPercent = 100 },
            new() { Code = "refeed_diet_break", Description = "Sugerencia automática de refeed tras 8 semanas", IsEnabled = false, RolloutPercent = 0 },
            new() { Code = "budget_modes_v1", Description = "Sistema de 6 modos de presupuesto", IsEnabled = true, RolloutPercent = 100 },
            new() { Code = "cost_estimation_v1", Description = "Estimación de costo en plan y lista de compras", IsEnabled = true, RolloutPercent = 100 },
            new() { Code = "savings_vs_gourmet_v1", Description = "Killer feature: ahorro vs modo gourmet", IsEnabled = true, RolloutPercent = 100 },
            new() { Code = "shopping_by_store_v1", Description = "Lista de compras agrupada por tienda", IsEnabled = true, RolloutPercent = 100 },
            new() { Code = "shopping_geolocation_v2", Description = "Tiendas reales por geolocalización (V2 con Google Places)", IsEnabled = false, RolloutPercent = 0 },
            new() { Code = "seasonal_pricing_v2", Description = "Precios por temporada y región (V2)", IsEnabled = false, RolloutPercent = 0 }
        };

        context.FeatureFlags.AddRange(flags);
        await context.SaveChangesAsync();
    }
}
