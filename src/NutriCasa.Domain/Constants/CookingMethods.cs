namespace NutriCasa.Domain.Constants;

/// <summary>
/// Métodos de cocción para recetas. String constants porque son editables desde BD.
/// </summary>
public static class CookingMethods
{
    public const string Stovetop = "stovetop";
    public const string Oven = "oven";
    public const string Microwave = "microwave";
    public const string Blender = "blender";
    public const string PressureCooker = "pressure_cooker";
    public const string NoCook = "no_cook";
    public const string Grill = "grill";
    public const string SlowCooker = "slow_cooker";

    public static readonly string[] All =
    [
        Stovetop, Oven, Microwave, Blender,
        PressureCooker, NoCook, Grill, SlowCooker
    ];
}
