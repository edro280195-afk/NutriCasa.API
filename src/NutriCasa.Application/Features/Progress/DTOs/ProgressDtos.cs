namespace NutriCasa.Application.Features.Progress.DTOs;

public record ProgressSummaryDto
{
    public double CurrentWeight { get; set; }
    public double StartWeight { get; set; }
    public double GoalWeight { get; set; }
    public double WeightChange { get; set; }
    public int StreakDays { get; set; }
    public double WeeklyAdherence { get; set; }
    public double OverallAdherence { get; set; }
    public string StartDate { get; set; } = "";
    public int CheckinsCompleted { get; set; }
    public int TotalCheckins { get; set; }
}

public record WeightEntryDto
{
    public string Date { get; set; } = "";
    public double HeightPercent { get; set; }
    public string Color { get; set; } = "";
}

public record CheckinDayDto
{
    public string Date { get; set; } = "";
    public int Level { get; set; }
}

public record WeeklyMacrosDto
{
    public MacroGoalDto Calories { get; set; } = new();
    public MacroGoalDto Protein { get; set; } = new();
    public MacroGoalDto Fat { get; set; } = new();
    public MacroGoalDto Carbs { get; set; } = new();
}

public record MacroGoalDto
{
    public double Current { get; set; }
    public double Goal { get; set; }
}
