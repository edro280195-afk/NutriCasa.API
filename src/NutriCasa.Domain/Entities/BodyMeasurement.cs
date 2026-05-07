using NutriCasa.Domain.Common;

namespace NutriCasa.Domain.Entities;

public class BodyMeasurement : BaseEntity
{
    public Guid UserId { get; set; }
    public decimal WeightKg { get; set; }
    public decimal? BodyFatPercentage { get; set; }
    public decimal? WaistCm { get; set; }
    public decimal? HipCm { get; set; }
    public decimal? NeckCm { get; set; }
    public decimal? ArmCm { get; set; }
    public decimal? ChestCm { get; set; }
    public decimal? ThighCm { get; set; }
    public string? Notes { get; set; }
    public DateOnly MeasuredAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
