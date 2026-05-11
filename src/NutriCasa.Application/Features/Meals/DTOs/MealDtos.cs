namespace NutriCasa.Application.Features.Meals.DTOs;

public record LogMealRequestDto
{
    public required string Status { get; init; }
    public string? SubstitutionNote { get; init; }
    public decimal? ActualPortion { get; init; }
}

public record MealLogDto
{
    public required Guid LogId { get; init; }
    public required Guid? PlanMealId { get; init; }
    public required string Status { get; init; }
    public string? SubstitutionNote { get; init; }
    public decimal? ActualPortion { get; init; }
    public required string RecipeName { get; init; }
    public required string MealType { get; init; }
    public required DateOnly LoggedForDate { get; init; }
    public required DateTime LoggedAt { get; init; }
}

public record MealAdherenceSummaryDto
{
    public required DateOnly Date { get; init; }
    public int TotalMeals { get; init; }
    public int CompletedMeals { get; init; }
    public int PartialMeals { get; init; }
    public int SkippedMeals { get; init; }
    public int SubstitutedMeals { get; init; }
    public decimal AdherencePercent { get; init; }
}
