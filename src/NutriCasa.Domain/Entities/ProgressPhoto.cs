using NutriCasa.Domain.Common;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Domain.Entities;

public class ProgressPhoto : SoftDeletableEntity
{
    public Guid UserId { get; set; }
    public string PhotoUrl { get; set; } = null!;
    public string StorageKey { get; set; } = null!;
    public PhotoAngle? Angle { get; set; }
    public VisibilityLevel Visibility { get; set; } = VisibilityLevel.Private;
    public Guid? AssociatedMeasurementId { get; set; }
    public long? FileSizeBytes { get; set; }
    public int? WidthPx { get; set; }
    public int? HeightPx { get; set; }
    public DateOnly TakenAt { get; set; }

    public User User { get; set; } = null!;
    public BodyMeasurement? AssociatedMeasurement { get; set; }
}
