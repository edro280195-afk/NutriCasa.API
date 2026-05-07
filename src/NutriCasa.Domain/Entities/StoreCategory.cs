using NutriCasa.Domain.Common;

namespace NutriCasa.Domain.Entities;

public class StoreCategory : AuditableEntity
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? ShortDescription { get; set; }
    public string? IconCode { get; set; }
    public decimal TypicalPriceFactor { get; set; } = 1.0m;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string[]? GooglePlaceTypes { get; set; }
}
