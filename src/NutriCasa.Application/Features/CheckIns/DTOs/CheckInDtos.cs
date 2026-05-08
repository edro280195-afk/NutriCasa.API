namespace NutriCasa.Application.Features.CheckIns.DTOs;

public record CreateCheckInRequest
{
    public int? HungerLevel { get; init; }
    public int? EnergyLevel { get; init; }
    public int? MoodLevel { get; init; }
    public int? DifficultyLevel { get; init; }
    public decimal? SleepHours { get; init; }
    public decimal? WaterLiters { get; init; }
    public bool HadCheatMeal { get; init; }
    public string? CheatDescription { get; init; }
    public string? Notes { get; init; }
    public DateOnly? CheckInDate { get; init; }
}

public record CheckInResponse
{
    public required Guid Id { get; init; }
    public required DateOnly CheckInDate { get; init; }
    public int? HungerLevel { get; init; }
    public int? EnergyLevel { get; init; }
    public int? MoodLevel { get; init; }
    public decimal? WaterLiters { get; init; }
    public required bool HadCheatMeal { get; init; }
    public string? Notes { get; init; }
    public required DateTime CreatedAt { get; init; }
}
