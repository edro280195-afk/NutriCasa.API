using Microsoft.EntityFrameworkCore;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Infrastructure.Persistence.Seeds;

public static class SystemThresholdSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.SystemThresholds.AnyAsync()) return;

        var thresholds = new List<SystemThreshold>
        {
            T("max_weekly_loss_warning_kg", "Pérdida semanal: warning", "Si la pérdida semanal supera este valor, mostrar alerta amarilla", "medical_safety", 1.0m, "kg/semana"),
            T("max_weekly_loss_blocking_kg", "Pérdida semanal: bloqueo", "Si la pérdida semanal supera este valor, sugerir aumentar calorías", "medical_safety", 1.5m, "kg/semana"),
            T("ketones_critical_threshold_mmol", "Cetonas: nivel crítico", "Por encima de este valor, mostrar alerta crítica", "medical_safety", 5.0m, "mmol/L"),
            T("ketones_emergency_threshold_mmol", "Cetonas: emergencia (T1/T2)", "Bloqueo de plan + mensaje de emergencia para diabéticos", "medical_safety", 8.0m, "mmol/L"),
            T("max_difficulty_consecutive_days", "Días consecutivos de dificultad alta", "Tras este número de días con dificultad ≥8, recalibrar plan", "medical_safety", 5, "días"),
            T("max_hunger_consecutive_days", "Días consecutivos de hambre alta", "Tras este número de días con hambre ≥8, recalibrar macros", "medical_safety", 5, "días"),
            T("plan_carbs_max_grams_keto", "Carbs máximos: keto estándar", "Límite de carbohidratos netos para validación de plan keto", "plan_validation", 50, "gramos/día"),
            T("plan_carbs_max_grams_override", "Carbs máximos: keto con override", "Límite de carbohidratos para usuarios con override médico", "plan_validation", 60, "gramos/día"),
            T("plan_carbs_max_grams_balanced", "Carbs máximos: alimentación balanceada", "Límite de carbohidratos para track balanced (menores V2)", "plan_validation", 150, "gramos/día"),
            T("bmr_calorie_floor_factor", "Piso calórico (% de BMR)", "Calorías diarias nunca por debajo de este factor del BMR", "plan_validation", 0.85m, "factor"),
            T("tdee_calorie_ceiling_factor", "Techo calórico (% de TDEE)", "Calorías diarias nunca por encima de este factor del TDEE", "plan_validation", 1.10m, "factor"),
            T("minimum_protein_per_kg", "Proteína mínima por kg de peso", "Gramos de proteína por kg de peso corporal", "plan_validation", 0.8m, "g/kg"),
            T("macros_validation_tolerance_percent", "Tolerancia de validación de macros", "Las macros del plan deben sumar 100% ± este porcentaje", "plan_validation", 2.0m, "%"),
            T("macros_visual_warning_yellow_percent", "Macros: warning visual amarillo", "Si el día se desvía más de este % del target, mostrar amarillo", "plan_validation", 15, "%"),
            T("macros_visual_warning_red_percent", "Macros: warning visual rojo", "Si el día se desvía más de este % del target, mostrar rojo", "plan_validation", 25, "%"),
            T("max_posts_per_user_hour", "Posts máximos por usuario/hora", "Rate limit de posts en muros grupales", "rate_limit", 10, "posts"),
            T("max_comments_per_user_hour", "Comentarios máximos por usuario/hora", "Rate limit de comentarios", "rate_limit", 30, "comentarios"),
            T("max_photos_per_user_day", "Fotos máximas por usuario/día", "Rate limit de subida de fotos de progreso", "rate_limit", 20, "fotos"),
            T("max_push_notifications_per_user_hour", "Push máximos por usuario/hora", "Throttling de notificaciones push (excepto P0)", "notification", 5, "notificaciones"),
            T("max_push_notifications_per_user_day", "Push máximos por usuario/día", "Throttling diario de push (excepto P0)", "notification", 15, "notificaciones"),
            T("failed_login_lock_threshold", "Intentos fallidos para lock corto", "Tras este número de fallos, bloquear 15 min", "auth_security", 5, "intentos"),
            T("failed_login_lock_minutes", "Duración del lock corto", "Minutos de bloqueo tras fallos", "auth_security", 15, "minutos"),
            T("failed_login_block_threshold", "Intentos fallidos para bloqueo total", "Tras este número de fallos en 24h, bloquear hasta reset", "auth_security", 10, "intentos"),
            T("failed_login_block_hours", "Ventana del contador de fallos", "Ventana de tiempo donde se cuentan los fallos", "auth_security", 24, "horas"),
            T("email_verification_expiry_hours", "Expiración del token de verificación de email", "Horas de validez del token", "auth_security", 24, "horas"),
            T("password_reset_expiry_hours", "Expiración del token de reset de password", "Horas de validez del token", "auth_security", 1, "horas"),
            T("refresh_token_expiry_days", "Expiración de refresh token", "Días de validez del refresh token JWT", "auth_security", 30, "días"),
            T("access_token_expiry_minutes", "Expiración de access token", "Minutos de validez del access token JWT", "auth_security", 15, "minutos"),
            T("account_deletion_grace_days", "Gracia de borrado de cuenta", "Días entre solicitud y borrado definitivo", "auth_security", 30, "días"),
            T("invite_code_expiry_days", "Expiración de invite code sin uso", "Días tras los cuales un invite code sin uso expira", "auth_security", 7, "días"),
            T("gemini_cache_ttl_days", "TTL del cache de respuestas Gemini", "Días que se mantiene viva una respuesta cacheada", "ai_budget", 7, "días"),
            T("circuit_breaker_error_rate_percent", "Circuit breaker: error rate", "Si Gemini supera este % de error en 5 min, abrir circuit", "ai_budget", 30, "%"),
            T("circuit_breaker_p95_latency_ms", "Circuit breaker: latencia p95", "Si la latencia p95 supera este ms, abrir circuit", "ai_budget", 30000, "ms"),
            T("ai_budget_warning_percent", "Budget IA: warning", "Email a admin al alcanzar este % del budget mensual", "ai_budget", 50, "%"),
            T("ai_budget_critical_percent", "Budget IA: crítico", "Email + Slack al alcanzar este % del budget mensual", "ai_budget", 80, "%"),
            T("max_user_ai_cost_monthly_usd", "Costo IA máximo por usuario/mes", "Por encima, investigar (posible abuso)", "ai_budget", 5.0m, "USD"),
            T("moderation_min_post_length", "Longitud mínima para moderación capa 2", "Posts más cortos solo pasan por capa 1", "moderation", 200, "caracteres"),
            T("min_check_in_streak_for_milestone", "Streak mínimo para milestone semanal", "Días consecutivos de check-in para hitos", "milestones", 7, "días"),
            T("weeks_continuous_deficit_for_refeed", "Semanas continuas para sugerir refeed", "Tras este número de semanas en déficit, sugerir diet break", "milestones", 8, "semanas"),
            T("weeks_no_change_for_plateau", "Semanas sin cambio para meseta", "Tras este número de semanas sin cambio, marcar plateau", "milestones", 4, "semanas")
        };

        context.SystemThresholds.AddRange(thresholds);
        await context.SaveChangesAsync();
    }

    private static SystemThreshold T(string code, string name, string desc, string cat, decimal val, string unit) =>
        new() { Code = code, Name = name, Description = desc, Category = cat, NumericValue = val, Unit = unit };
}
