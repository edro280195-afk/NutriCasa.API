namespace NutriCasa.Domain.Constants;

/// <summary>
/// Categorías de ingredientes del catálogo. String constants porque son editables desde BD.
/// </summary>
public static class IngredientCategories
{
    public const string Protein = "protein";
    public const string Dairy = "dairy";
    public const string Vegetable = "vegetable";
    public const string Fruit = "fruit";
    public const string Fat = "fat";
    public const string Seasoning = "seasoning";
    public const string Beverage = "beverage";
    public const string Grain = "grain";
    public const string NutSeed = "nut_seed";
    public const string Condiment = "condiment";
    public const string Canned = "canned";
    public const string Frozen = "frozen";
    public const string Other = "other";

    public static readonly string[] All =
    [
        Protein, Dairy, Vegetable, Fruit, Fat, Seasoning,
        Beverage, Grain, NutSeed, Condiment, Canned, Frozen, Other
    ];
}
