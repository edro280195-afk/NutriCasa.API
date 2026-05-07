using NutriCasa.Domain.Common;

namespace NutriCasa.Domain.Entities;

public class IngredientCatalog : AuditableEntity
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? NameSearch { get; set; }
    public string Category { get; set; } = null!; // String, no enum: editable catalog
    public decimal? KcalPer100g { get; set; }
    public decimal? ProteinGPer100g { get; set; }
    public decimal? FatGPer100g { get; set; }
    public decimal? CarbsGPer100g { get; set; }
    public decimal? FiberGPer100g { get; set; }
    public decimal? AvgPriceMxn { get; set; }
    public string? AvgPriceUnit { get; set; }
    public decimal? UnitGramsEquivalent { get; set; }
    public DateOnly? LastPriceUpdate { get; set; }
    public int EconomicTier { get; set; } = 3;
    public string[] Tags { get; set; } = [];
    public string? PrimaryStoreCategory { get; set; }
    public string? SecondaryStoreCategory { get; set; }
    public bool IsSeasonal { get; set; }
    public int[] SeasonalMonths { get; set; } = [];
    public bool IsKetoFriendly { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public Guid? UpdatedByUserId { get; set; }

    public User? UpdatedByUser { get; set; }
}
