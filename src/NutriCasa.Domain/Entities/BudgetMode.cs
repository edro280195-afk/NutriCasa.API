using NutriCasa.Domain.Common;

namespace NutriCasa.Domain.Entities;

public class BudgetMode : AuditableEntity
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string ShortDescription { get; set; } = null!;
    public string? LongDescription { get; set; }
    public string? IconCode { get; set; }
    public string? ColorTheme { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal? EstimatedCostMinMxn { get; set; }
    public decimal? EstimatedCostMaxMxn { get; set; }
    public string Rules { get; set; } = "{}"; // JSONB
}
