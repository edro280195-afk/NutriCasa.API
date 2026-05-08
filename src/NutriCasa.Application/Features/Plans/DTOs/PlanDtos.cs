namespace NutriCasa.Application.Features.Plans.DTOs;

public record GeneratePlanRequestDto
{
    public required DateOnly WeekStartDate { get; init; }
    public bool ForceRegenerate { get; init; }
}
