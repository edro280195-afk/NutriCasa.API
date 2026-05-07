using NutriCasa.Domain.Common;

namespace NutriCasa.Domain.Entities;

public class ShoppingListItem : BaseEntity
{
    public Guid ShoppingListId { get; set; }
    public string IngredientName { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public string Unit { get; set; } = null!;
    public string? Category { get; set; }
    public decimal? EstimatedCostMxn { get; set; }
    public bool IsPurchased { get; set; }
    public Guid? PurchasedByUserId { get; set; }
    public DateTime? PurchasedAt { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
    // Del delta 002
    public Guid? IngredientId { get; set; }
    public Guid? StoreCategoryId { get; set; }
    public Guid? SubstitutedFromId { get; set; }
    public string? SubstitutionReason { get; set; }

    public ShoppingList ShoppingList { get; set; } = null!;
    public User? PurchasedByUser { get; set; }
    public IngredientCatalog? Ingredient { get; set; }
    public StoreCategory? StoreCategory { get; set; }
    public IngredientCatalog? SubstitutedFrom { get; set; }
}
