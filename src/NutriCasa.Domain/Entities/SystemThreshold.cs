using NutriCasa.Domain.Common;

namespace NutriCasa.Domain.Entities;

public class SystemThreshold : AuditableEntity
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Category { get; set; } = null!;
    public decimal? NumericValue { get; set; }
    public string? TextValue { get; set; }
    public string? Unit { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? UpdatedByUserId { get; set; }

    public User? UpdatedByUser { get; set; }
}
