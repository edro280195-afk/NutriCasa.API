using NutriCasa.Domain.Common;

namespace NutriCasa.Domain.Entities;

public class WeeklySummary : BaseEntity
{
    public Guid UserId { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public decimal? AvgDifficulty { get; set; }
    public decimal? AvgHunger { get; set; }
    public decimal? AvgEnergy { get; set; }
    public decimal? AdherencePercent { get; set; }
    public decimal? WeightChangeKg { get; set; }
    public int? CheckInsCount { get; set; }
    public int? MealsCompleted { get; set; }
    public int? MealsPlanned { get; set; }
    public bool IsInPlateau { get; set; }
    public string? AiRecommendations { get; set; } // JSONB
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
