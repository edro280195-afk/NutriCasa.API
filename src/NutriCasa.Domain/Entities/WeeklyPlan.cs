using NutriCasa.Domain.Common;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Domain.Entities;

public class WeeklyPlan : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid? GroupId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public NutritionTrack NutritionTrack { get; set; } = NutritionTrack.Keto;
    public bool IsOverridePlan { get; set; }
    public bool IsRefeedWeek { get; set; }
    public string OriginalMenuContent { get; set; } = "{}"; // JSONB
    public string? CurrentMenuContent { get; set; } // JSONB
    public bool IsActive { get; set; } = true;
    public GenerationSource GenerationSource { get; set; } = GenerationSource.Ai;
    public Guid? ParentPlanId { get; set; }
    public Guid? AiInteractionId { get; set; }
    // Del delta 002
    public Guid? BudgetModeId { get; set; }
    public decimal? EstimatedTotalCostMxn { get; set; }
    public decimal? EstimatedCostPerPersonMxn { get; set; }
    public decimal? EstimatedCostGourmetBaselineMxn { get; set; }
    public decimal? SavingsVsGourmetMxn { get; set; }
    public decimal? SavingsVsGourmetPercent { get; set; }

    public User User { get; set; } = null!;
    public Group? Group { get; set; }
    public WeeklyPlan? ParentPlan { get; set; }
    public AiInteraction? AiInteraction { get; set; }
    public BudgetMode? BudgetMode { get; set; }
    public ICollection<WeeklyPlanMeal> Meals { get; set; } = new List<WeeklyPlanMeal>();
}
