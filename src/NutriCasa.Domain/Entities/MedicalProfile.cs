using NutriCasa.Domain.Common;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Domain.Entities;

public class MedicalProfile : AuditableEntity
{
    public Guid UserId { get; set; }
    public bool HasDiabetes { get; set; }
    public DiabetesType? DiabetesType { get; set; }
    public bool IsPregnantOrLactating { get; set; }
    public bool HasKidneyIssues { get; set; }
    public bool HasLiverIssues { get; set; }
    public bool HasPancreasIssues { get; set; }
    public bool HasThyroidIssues { get; set; }
    public bool HasHeartCondition { get; set; }
    public bool HasEatingDisorderHistory { get; set; }
    public bool HasGallbladderIssues { get; set; }
    public string? OtherConditions { get; set; }
    public string[] Allergies { get; set; } = [];
    public string[] Medications { get; set; } = [];
    public string[] DietaryRestrictions { get; set; } = [];
    public string[] DislikedIngredients { get; set; } = [];
    public string[] PreferredIngredients { get; set; } = [];
    public KetoExperienceLevel KetoExperienceLevel { get; set; } = KetoExperienceLevel.Beginner;
    public bool RequiresHumanReview { get; set; }
    public DateTime? HumanReviewCompletedAt { get; set; }
    public string? HumanReviewNotes { get; set; }
    public DateTime? OverrideAcceptedAt { get; set; }
    public Guid? OverrideDisclaimerVersionId { get; set; }
    public DateTime? OverrideRevokedAt { get; set; }

    public User User { get; set; } = null!;
    public DisclaimerVersion? OverrideDisclaimerVersion { get; set; }
}
