using NutriCasa.Domain.Common;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Domain.Entities;

public class MealLog : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? PlanMealId { get; set; }
    public Guid? RecipeId { get; set; }
    public MealLogStatus Status { get; set; }
    public string? SubstitutionNote { get; set; }
    public decimal? ActualPortion { get; set; }
    public DateOnly LoggedForDate { get; set; }
    public DateTime LoggedAt { get; set; }

    public User User { get; set; } = null!;
    public WeeklyPlanMeal? PlanMeal { get; set; }
    public Recipe? Recipe { get; set; }
}
