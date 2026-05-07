using Microsoft.EntityFrameworkCore;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Infrastructure.Persistence.Seeds;

public static class ToxicWordSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.ToxicWords.AnyAsync()) return;
        context.ToxicWords.AddRange(
            new ToxicWord { Word = "CURP_PATTERN", NormalizedWord = "curp_pattern", Category = ToxicWordCategory.DoxingPattern, Severity = ToxicWordSeverity.High, Language = "es-MX", IsRegex = true, Pattern = @"\b[A-Z]{4}[0-9]{6}[HM][A-Z]{5}[0-9A-Z][0-9]\b" },
            new ToxicWord { Word = "RFC_PATTERN", NormalizedWord = "rfc_pattern", Category = ToxicWordCategory.DoxingPattern, Severity = ToxicWordSeverity.High, Language = "es-MX", IsRegex = true, Pattern = @"\b[A-ZÑ&]{3,4}[0-9]{6}[A-Z0-9]{3}\b" },
            new ToxicWord { Word = "PHONE_MX_PATTERN", NormalizedWord = "phone_mx_pattern", Category = ToxicWordCategory.DoxingPattern, Severity = ToxicWordSeverity.Medium, Language = "es-MX", IsRegex = true, Pattern = @"\b(?:\+52)?[\s-]?(?:1[\s-]?)?(?:\d{3}[\s-]?\d{3}[\s-]?\d{4}|\d{2}[\s-]?\d{4}[\s-]?\d{4})\b" },
            new ToxicWord { Word = "compra_mi_link", NormalizedWord = "compra mi link", Category = ToxicWordCategory.Spam, Severity = ToxicWordSeverity.Medium, Language = "es-MX", IsRegex = false },
            new ToxicWord { Word = "link_de_afiliado", NormalizedWord = "link de afiliado", Category = ToxicWordCategory.Spam, Severity = ToxicWordSeverity.Medium, Language = "es-MX", IsRegex = false },
            new ToxicWord { Word = "PLACEHOLDER_DISCRIMINATION_1", NormalizedWord = "placeholder_discrimination_1", Category = ToxicWordCategory.Discrimination, Severity = ToxicWordSeverity.High, Language = "es-MX", IsRegex = false },
            new ToxicWord { Word = "dejar_de_tomar_medicamento", NormalizedWord = "dejar de tomar medicamento", Category = ToxicWordCategory.MedicalDangerous, Severity = ToxicWordSeverity.High, Language = "es-MX", IsRegex = false },
            new ToxicWord { Word = "ayuno_extremo_dias", NormalizedWord = "ayuno extremo de varios dias", Category = ToxicWordCategory.MedicalDangerous, Severity = ToxicWordSeverity.High, Language = "es-MX", IsRegex = false }
        );
        await context.SaveChangesAsync();
    }
}
