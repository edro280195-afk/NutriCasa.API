namespace NutriCasa.Application.Features.Subscriptions.DTOs;

public record SubscriptionPlanDto
{
    public required Guid PlanId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required decimal PriceMonthlyMxn { get; init; }
    public decimal? PriceYearlyMxn { get; init; }
    public required int TrialDays { get; init; }
    public int? MaxGroupMembers { get; init; }
    public int? MaxRegenerationsWeek { get; init; }
    public int? MaxSwapsWeek { get; init; }
    public bool HasAiChat { get; init; }
    public bool HasPhotoAnalysis { get; init; }
    public required int SortOrder { get; init; }
}

public record UserSubscriptionDto
{
    public required Guid SubscriptionId { get; init; }
    public required Guid PlanId { get; init; }
    public required string PlanCode { get; init; }
    public required string PlanName { get; init; }
    public required decimal PriceMonthlyMxn { get; init; }
    public required string Status { get; init; }
    public required DateTime StartedAt { get; init; }
    public DateTime? CurrentPeriodEnd { get; init; }
    public bool CancelAtPeriodEnd { get; init; }
}

public record CreateCheckoutRequestDto
{
    public required Guid PlanId { get; init; }
}

public record TrialSubscriptionRequestDto
{
    public required Guid PlanId { get; init; }
}
