using Microsoft.EntityFrameworkCore;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Infrastructure.Persistence.Seeds;

public static class SubscriptionPlanSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.SubscriptionPlans.AnyAsync()) return;

        var plans = new List<SubscriptionPlan>
        {
            new()
            {
                Code = "free", Name = "Gratis",
                Description = "Para probar la plataforma con tu familia más cercana.",
                PriceMonthlyMxn = 0, PriceYearlyMxn = 0, TrialDays = 0,
                MaxGroupMembers = 5, MaxRegenerationsWeek = 1, MaxSwapsWeek = 5, MaxChatMessagesMonth = 0,
                HasAiChat = false, HasPhotoAnalysis = false, HasAdvancedAnalytics = false, HasPrioritySupport = false,
                SortOrder = 1
            },
            new()
            {
                Code = "family", Name = "Familia",
                Description = "Para retos familiares completos con todas las herramientas.",
                PriceMonthlyMxn = 149, PriceYearlyMxn = 1490, TrialDays = 14,
                MaxGroupMembers = null, MaxRegenerationsWeek = 4, MaxSwapsWeek = null, MaxChatMessagesMonth = 100,
                HasAiChat = true, HasPhotoAnalysis = false, HasAdvancedAnalytics = true, HasPrioritySupport = false,
                SortOrder = 2
            },
            new()
            {
                Code = "pro", Name = "Pro",
                Description = "Coach IA ilimitado, análisis de fotos de comida y soporte prioritario.",
                PriceMonthlyMxn = 299, PriceYearlyMxn = 2990, TrialDays = 0,
                MaxGroupMembers = null, MaxRegenerationsWeek = null, MaxSwapsWeek = null, MaxChatMessagesMonth = null,
                HasAiChat = true, HasPhotoAnalysis = true, HasAdvancedAnalytics = true, HasPrioritySupport = true,
                SortOrder = 3
            }
        };

        context.SubscriptionPlans.AddRange(plans);
        await context.SaveChangesAsync();
    }
}
