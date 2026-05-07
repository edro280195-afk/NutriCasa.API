using NutriCasa.Domain.Common;

namespace NutriCasa.Domain.Entities;

public class ShoppingList : BaseEntity
{
    public Guid GroupId { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public decimal? TotalEstimatedMxn { get; set; }
    public string? Notes { get; set; }
    public DateTime GeneratedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    // Del delta 002
    public Guid? BudgetModeId { get; set; }
    public decimal? TotalEstimatedCostMxn { get; set; }
    public string? CostBreakdown { get; set; } // JSONB

    public Group Group { get; set; } = null!;
    public BudgetMode? BudgetMode { get; set; }
    public ICollection<ShoppingListItem> Items { get; set; } = new List<ShoppingListItem>();
}
