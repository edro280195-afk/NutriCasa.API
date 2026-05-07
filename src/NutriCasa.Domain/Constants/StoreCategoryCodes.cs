namespace NutriCasa.Domain.Constants;

/// <summary>
/// Códigos de categorías de tienda. String constants porque son editables desde BD.
/// </summary>
public static class StoreCategoryCodes
{
    public const string MercadoTradicional = "mercado_tradicional";
    public const string Supermercado = "supermercado";
    public const string Tianguis = "tianguis";
    public const string TiendaEsquina = "tienda_esquina";

    public static readonly string[] All =
    [
        MercadoTradicional, Supermercado, Tianguis, TiendaEsquina
    ];
}
