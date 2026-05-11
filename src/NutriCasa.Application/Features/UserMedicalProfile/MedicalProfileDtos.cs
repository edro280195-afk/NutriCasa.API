namespace NutriCasa.Application.Features.UserMedicalProfile.DTOs;

public record MedicalProfileDto
{
    public bool HasDiabetes { get; init; }
    public string? DiabetesType { get; init; }
    public bool IsPregnantOrLactating { get; init; }
    public bool HasKidneyIssues { get; init; }
    public bool HasLiverIssues { get; init; }
    public bool HasPancreasIssues { get; init; }
    public bool HasThyroidIssues { get; init; }
    public bool HasHeartCondition { get; init; }
    public bool HasEatingDisorderHistory { get; init; }
    public bool HasGallbladderIssues { get; init; }
    public string? OtherConditions { get; init; }
    public string[] Allergies { get; init; } = [];
    public string[] Medications { get; init; } = [];
    public string[] DietaryRestrictions { get; init; } = [];
    public string[] DislikedIngredients { get; init; } = [];
    public string[] PreferredIngredients { get; init; } = [];
    public string KetoExperienceLevel { get; init; } = "Beginner";
    public bool RequiresHumanReview { get; init; }
    public DateTime? OverrideAcceptedAt { get; init; }
}

public record UpdateMedicalProfileRequest
{
    public bool HasDiabetes { get; init; }
    public string? DiabetesType { get; init; }
    public bool IsPregnantOrLactating { get; init; }
    public bool HasKidneyIssues { get; init; }
    public bool HasLiverIssues { get; init; }
    public bool HasPancreasIssues { get; init; }
    public bool HasThyroidIssues { get; init; }
    public bool HasHeartCondition { get; init; }
    public bool HasEatingDisorderHistory { get; init; }
    public bool HasGallbladderIssues { get; init; }
    public string? OtherConditions { get; init; }
    public string[] Allergies { get; init; } = [];
    public string[] Medications { get; init; } = [];
    public string[] DietaryRestrictions { get; init; } = [];
    public string[] DislikedIngredients { get; init; } = [];
    public string[] PreferredIngredients { get; init; } = [];
    public string KetoExperienceLevel { get; init; } = "Beginner";
}
