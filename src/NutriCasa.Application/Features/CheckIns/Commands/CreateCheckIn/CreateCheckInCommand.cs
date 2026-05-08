using MediatR;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.CheckIns.DTOs;

namespace NutriCasa.Application.Features.CheckIns.Commands.CreateCheckIn;

public record CreateCheckInCommand : IRequest<Result<CheckInResponse>>
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
