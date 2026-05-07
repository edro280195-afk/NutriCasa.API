using NutriCasa.Domain.Common;

namespace NutriCasa.Domain.Entities;

public class KetoProfile : AuditableEntity
{
    public Guid UserId { get; set; }
    public int? BmrKcal { get; set; }
    public int? TdeeKcal { get; set; }
    public int DailyCalories { get; set; }
    public decimal CarbsGrams { get; set; }
    public decimal ProteinGrams { get; set; }
    public decimal FatGrams { get; set; }
    public decimal? CarbsPercent { get; set; }
    public decimal? ProteinPercent { get; set; }
    public decimal? FatPercent { get; set; }
    public string CalculationMethod { get; set; } = "mifflin_st_jeor";
    public DateTime LastCalculatedAt { get; set; }
    // Del delta 002
    public decimal? TargetWeeklyCostMxn { get; set; }
    public decimal? TargetMealCostMxn { get; set; }

    public User User { get; set; } = null!;
}
