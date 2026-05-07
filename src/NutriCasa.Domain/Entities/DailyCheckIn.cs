using NutriCasa.Domain.Common;

namespace NutriCasa.Domain.Entities;

public class DailyCheckIn : BaseEntity
{
    public Guid UserId { get; set; }
    public DateOnly CheckInDate { get; set; }
    public int? HungerLevel { get; set; }
    public int? EnergyLevel { get; set; }
    public int? MoodLevel { get; set; }
    public int? DifficultyLevel { get; set; }
    public decimal? SleepHours { get; set; }
    public decimal? WaterLiters { get; set; }
    public decimal? KetonesMmol { get; set; }
    public bool HadCheatMeal { get; set; }
    public string? CheatDescription { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
