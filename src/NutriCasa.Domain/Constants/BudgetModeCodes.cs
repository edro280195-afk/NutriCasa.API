namespace NutriCasa.Domain.Constants;

/// <summary>
/// Códigos de los 6 modos de presupuesto. String constants porque son editables desde BD.
/// </summary>
public static class BudgetModeCodes
{
    public const string Economic = "economic";
    public const string PantryBasic = "pantry_basic";
    public const string SimpleKitchen = "simple_kitchen";
    public const string BusyParent = "busy_parent";
    public const string Athletic = "athletic";
    public const string Gourmet = "gourmet";

    public static readonly string[] All =
    [
        Economic, PantryBasic, SimpleKitchen,
        BusyParent, Athletic, Gourmet
    ];
}
