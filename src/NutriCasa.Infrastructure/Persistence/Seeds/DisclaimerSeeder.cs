using Microsoft.EntityFrameworkCore;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Infrastructure.Persistence.Seeds;

public static class DisclaimerSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.DisclaimerVersions.AnyAsync()) return;

        var disclaimers = new List<DisclaimerVersion>
        {
            new()
            {
                DisclaimerType = "general",
                VersionCode = "1.0",
                Title = "Aviso Médico y Términos de Uso de NutriCasa",
                Content = "# Aviso Médico Importante\n\n**NutriCasa no es un servicio médico ni nutricional profesional.** Los planes alimenticios, recomendaciones y contenido generados por inteligencia artificial son puramente informativos y educativos. No constituyen consejo médico, diagnóstico ni tratamiento.\n\n## Antes de comenzar una dieta cetogénica:\n\n- Consulta con tu médico, especialmente si tienes diabetes, problemas renales, hepáticos, pancreáticos, cardíacos, tiroideos, antecedentes de trastornos alimenticios, embarazo o lactancia.\n- Informa a tu médico sobre cualquier medicamento que tomes.\n- Si experimentas mareos severos, dolor de pecho, dificultad para respirar o cualquier síntoma preocupante, suspende la dieta y busca atención médica inmediata.\n\n## Limitación de responsabilidad\n\nAl aceptar estos términos, reconoces que el uso de NutriCasa es bajo tu propia responsabilidad.\n\n## Privacidad\n\nTus datos médicos y biométricos se almacenan cifrados y nunca se comparten con terceros sin tu consentimiento explícito.",
                EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow),
                IsCurrent = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                DisclaimerType = "override",
                VersionCode = "1.0",
                Title = "Disclaimer Adicional para Override Médico",
                Content = "# Aceptación de Riesgos para Override Médico\n\n**Estás a punto de continuar con un plan cetogénico a pesar de tener una condición médica que requiere atención especializada.**\n\n## Reconozco y entiendo que:\n\n1. Mi condición médica puede tener interacciones serias con la dieta cetogénica.\n2. La dieta puede alterar la dosis y efectos de mis medicamentos.\n3. El plan tendrá macros conservadoras (carbohidratos hasta 60g/día) para reducir el impacto.\n4. Debo monitorear señales de alerta.\n5. Ante cualquier síntoma preocupante, debo suspender la dieta inmediatamente.\n\n## Mi compromiso:\n\n- Mantener informado a mi médico.\n- Realizar revisiones médicas periódicas (mínimo cada 4 semanas).\n- Reportar honestamente mis síntomas en los check-ins diarios.\n- No usar este plan como sustituto de tratamiento médico profesional.",
                EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow),
                IsCurrent = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                DisclaimerType = "tutor",
                VersionCode = "1.0",
                Title = "Consentimiento de Tutor para Menor de Edad (V2)",
                Content = "# Consentimiento Informado de Tutor\n\n**Este disclaimer aplica para tutores legales de menores de 16-17 años que serán usuarios de NutriCasa en modalidad de alimentación balanceada (NO cetogénica).**\n\n## Como tutor legal del menor, declaro que:\n\n1. Tengo la patria potestad o tutoría legal del menor.\n2. Entiendo que el menor recibirá un plan de alimentación balanceada, NO cetogénico.\n3. He consultado con un pediatra o médico de cabecera del menor.\n4. Asumo la responsabilidad de supervisar el seguimiento del plan.\n5. Tendré acceso completo a las métricas y progreso del menor.\n\n## Compromiso de privacidad:\n\nLos datos del menor se manejan con privacidad reforzada conforme a la LFPDPPP.",
                EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow),
                IsCurrent = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.DisclaimerVersions.AddRange(disclaimers);
        await context.SaveChangesAsync();
    }
}
