namespace NutriCasa.Domain.Constants;

/// <summary>
/// Códigos de los umbrales del sistema configurables. Cada código corresponde a un registro en system_thresholds.
/// </summary>
public static class SystemThresholdCodes
{
    // Seguridad médica
    public const string MaxWeeklyLossWarningKg = "max_weekly_loss_warning_kg";
    public const string MaxWeeklyLossBlockingKg = "max_weekly_loss_blocking_kg";
    public const string KetonesCriticalThresholdMmol = "ketones_critical_threshold_mmol";
    public const string KetonesEmergencyThresholdMmol = "ketones_emergency_threshold_mmol";
    public const string MaxDifficultyConsecutiveDays = "max_difficulty_consecutive_days";
    public const string MaxHungerConsecutiveDays = "max_hunger_consecutive_days";

    // Validación de plan
    public const string PlanCarbsMaxGramsKeto = "plan_carbs_max_grams_keto";
    public const string PlanCarbsMaxGramsOverride = "plan_carbs_max_grams_override";
    public const string PlanCarbsMaxGramsBalanced = "plan_carbs_max_grams_balanced";
    public const string BmrCalorieFloorFactor = "bmr_calorie_floor_factor";
    public const string TdeeCalorieCeilingFactor = "tdee_calorie_ceiling_factor";
    public const string MinimumProteinPerKg = "minimum_protein_per_kg";
    public const string MacrosValidationTolerancePercent = "macros_validation_tolerance_percent";
    public const string MacrosVisualWarningYellowPercent = "macros_visual_warning_yellow_percent";
    public const string MacrosVisualWarningRedPercent = "macros_visual_warning_red_percent";

    // Rate limits
    public const string MaxPostsPerUserHour = "max_posts_per_user_hour";
    public const string MaxCommentsPerUserHour = "max_comments_per_user_hour";
    public const string MaxPhotosPerUserDay = "max_photos_per_user_day";

    // Notificaciones
    public const string MaxPushNotificationsPerUserHour = "max_push_notifications_per_user_hour";
    public const string MaxPushNotificationsPerUserDay = "max_push_notifications_per_user_day";

    // Auth y seguridad
    public const string FailedLoginLockThreshold = "failed_login_lock_threshold";
    public const string FailedLoginLockMinutes = "failed_login_lock_minutes";
    public const string FailedLoginBlockThreshold = "failed_login_block_threshold";
    public const string FailedLoginBlockHours = "failed_login_block_hours";
    public const string EmailVerificationExpiryHours = "email_verification_expiry_hours";
    public const string PasswordResetExpiryHours = "password_reset_expiry_hours";
    public const string RefreshTokenExpiryDays = "refresh_token_expiry_days";
    public const string AccessTokenExpiryMinutes = "access_token_expiry_minutes";
    public const string AccountDeletionGraceDays = "account_deletion_grace_days";
    public const string InviteCodeExpiryDays = "invite_code_expiry_days";

    // IA y costos
    public const string GeminiCacheTtlDays = "gemini_cache_ttl_days";
    public const string CircuitBreakerErrorRatePercent = "circuit_breaker_error_rate_percent";
    public const string CircuitBreakerP95LatencyMs = "circuit_breaker_p95_latency_ms";
    public const string AiBudgetWarningPercent = "ai_budget_warning_percent";
    public const string AiBudgetCriticalPercent = "ai_budget_critical_percent";
    public const string MaxUserAiCostMonthlyUsd = "max_user_ai_cost_monthly_usd";

    // Moderación
    public const string ModerationMinPostLength = "moderation_min_post_length";

    // Milestones
    public const string MinCheckInStreakForMilestone = "min_check_in_streak_for_milestone";
    public const string WeeksContinuousDeficitForRefeed = "weeks_continuous_deficit_for_refeed";
    public const string WeeksNoChangeForPlateau = "weeks_no_change_for_plateau";
}
