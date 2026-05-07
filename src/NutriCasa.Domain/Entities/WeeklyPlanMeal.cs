using NutriCasa.Domain.Common;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Domain.Entities;

public class WeeklyPlanMeal : AuditableEntity
{
    public Guid PlanId { get; set; }
    public int DayOfWeek { get; set; }
    public MealType MealType { get; set; }
    public Guid? RecipeId { get; set; }
    public decimal PortionMultiplier { get; set; } = 1.0m;
    public bool IsLocked { get; set; }
    public string? UserNote { get; set; }
    public int SortOrder { get; set; }
    public long RowVersion { get; set; } = 1;

    public WeeklyPlan Plan { get; set; } = null!;
    public Recipe? Recipe { get; set; }
}
