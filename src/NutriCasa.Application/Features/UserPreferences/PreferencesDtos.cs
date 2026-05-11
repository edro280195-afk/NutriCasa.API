namespace NutriCasa.Application.Features.UserPreferences.DTOs;

public record PreferencesDto
{
    /* Privacy */
    public string ShareWeight { get; init; } = "Private";
    public string ShareBodyFat { get; init; } = "Private";
    public string ShareMeasurements { get; init; } = "Private";
    public string SharePhotos { get; init; } = "Private";
    public string ShareCheckIns { get; init; } = "Group";
    public bool AllowAiMentions { get; init; } = true;

    /* Notifications */
    public bool AllowPush { get; init; } = true;
    public bool AllowEmail { get; init; } = true;
    public bool WeeklyDigest { get; init; } = true;
    public string QuietHoursStart { get; init; } = "21:00";
    public string QuietHoursEnd { get; init; } = "08:00";

    /* User settings */
    public string Timezone { get; init; } = "America/Mexico_City";
    public string PreferredLanguage { get; init; } = "es-MX";
    public string NutritionTrack { get; init; } = "Keto";
    public string? BudgetModeCode { get; init; }
    public string? BudgetModeName { get; init; }
}

public record UpdatePreferencesRequest
{
    /* Privacy */
    public string? ShareWeight { get; init; }
    public string? ShareBodyFat { get; init; }
    public string? ShareMeasurements { get; init; }
    public string? SharePhotos { get; init; }
    public string? ShareCheckIns { get; init; }
    public bool? AllowAiMentions { get; init; }

    /* Notifications */
    public bool? AllowPush { get; init; }
    public bool? AllowEmail { get; init; }
    public bool? WeeklyDigest { get; init; }
    public string? QuietHoursStart { get; init; }
    public string? QuietHoursEnd { get; init; }

    /* User settings */
    public string? Timezone { get; init; }
    public string? PreferredLanguage { get; init; }
}
