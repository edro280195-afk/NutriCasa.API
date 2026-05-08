using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.Onboarding.DTOs;

// ─── Step 1: Group ────────────────────────────────────────────────────────────
public record CompleteStep1GroupRequest
{
    public required string Action { get; init; } // "create" | "join"
    public string? GroupName { get; init; }
    public string? InviteCode { get; init; }
}

public record CompleteStep1GroupResponse
{
    public required Guid GroupId { get; init; }
    public string? GroupName { get; init; }
    public string? InviteCode { get; init; }
}

// ─── Step 2: Basic Data ───────────────────────────────────────────────────────
public record CompleteStep2BasicDataRequest
{
    public string? FullName { get; init; }
    public string? ProfilePhotoUrl { get; init; }
    public required DateOnly BirthDate { get; init; }
    public required string Gender { get; init; } // "Male", "Female", "NonBinary", "PreferNotToSay"
}

// ─── Step 3: Metrics ──────────────────────────────────────────────────────────
public record CompleteStep3MetricsRequest
{
    public required decimal HeightCm { get; init; }
    public required decimal WeightKg { get; init; }
    public decimal? TargetWeightKg { get; init; }
    public string? GoalType { get; init; } // "WeightLoss", "BodyRecomp", etc.
}

public record CompleteStep3MetricsResponse
{
    public string? WarningMessage { get; init; }
}

// ─── Step 4: Body Type ────────────────────────────────────────────────────────
public record CompleteStep4BodyTypeRequest
{
    public required string BodyType { get; init; } // "slim", "average", "athletic", "curvy", "plus", "heavy"
}

// ─── Step 5: Activity Level ───────────────────────────────────────────────────
public record CompleteStep5ActivityRequest
{
    public required string ActivityLevel { get; init; } // "Sedentary", "Light", "Moderate", "Active", "VeryActive"
}

// ─── Step 5.5: Budget Mode ────────────────────────────────────────────────────
public record CompleteStep5_5BudgetModeRequest
{
    public string? BudgetModeCode { get; init; }
}

// ─── Step 6: Medical Profile ──────────────────────────────────────────────────
public record CompleteStep6MedicalProfileRequest
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
    public required string KetoExperienceLevel { get; init; } // "Beginner", "Intermediate", "Advanced"
}

public record CompleteStep6MedicalProfileResponse
{
    public required bool RequiresOverride { get; init; }
    public string[] Conditions { get; init; } = [];
    public string? Message { get; init; }
}

// ─── Step 6.5: Medical Override ───────────────────────────────────────────────
public record CompleteStep6_5MedicalOverrideRequest
{
    public required string PasswordConfirmation { get; init; }
    public required bool DisclaimerAccepted { get; init; }
    public required Guid DisclaimerVersionId { get; init; }
}

// ─── Step 7: Disclaimer & Goal ────────────────────────────────────────────────
public record CompleteStep7DisclaimerGoalRequest
{
    public string? DisclaimerVersionId { get; init; }
    public required string GoalType { get; init; }
    public decimal? TargetWeightKg { get; init; }
    public DateOnly? TargetDate { get; init; }
    public string? MotivationText { get; init; }
}

public record CompleteStep7DisclaimerGoalResponse
{
    public required bool OnboardingComplete { get; init; }
    public required KetoProfileResult KetoProfile { get; init; }
}

// ─── Status ───────────────────────────────────────────────────────────────────
public record OnboardingStatusResponse
{
    public required StepsCompletedDto StepsCompleted { get; init; }
    public required bool RequiresOverride { get; init; }
    public required bool OnboardingComplete { get; init; }
    public required int CurrentSuggestedStep { get; init; }
}

public record StepsCompletedDto
{
    public bool Step1Group { get; init; }
    public bool Step2BasicData { get; init; }
    public bool Step3Metrics { get; init; }
    public bool Step4BodyType { get; init; }
    public bool Step5Activity { get; init; }
    public bool Step5BudgetMode { get; init; }
    public bool Step6MedicalProfile { get; init; }
    public bool Step6Override { get; init; }
    public bool Step7Disclaimer { get; init; }
}
