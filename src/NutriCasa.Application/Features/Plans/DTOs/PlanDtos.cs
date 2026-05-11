using NutriCasa.Application.Features.Plans.Commands.ReorderMeals;

namespace NutriCasa.Application.Features.Plans.DTOs;

public record GeneratePlanRequestDto
{
    public required DateOnly WeekStartDate { get; init; }
    public bool ForceRegenerate { get; init; }
}

public record ReorderMealsRequestDto
{
    public required List<MealMoveDto> Moves { get; init; }
}

public record MealMoveDto
{
    public required Guid PlanMealId { get; init; }
    public required int NewDayOfWeek { get; init; }
    public required string NewMealType { get; init; }
    public required long RowVersion { get; init; }
    public int NewSortOrder { get; init; } = 1;
}

public record ToggleLockRequestDto
{
    public required bool Locked { get; init; }
}

public record SwapMealRequestDto
{
    public string? Reason { get; init; }
}

public record AdjustPortionRequestDto
{
    public required decimal PortionMultiplier { get; init; }
}
