using Microsoft.EntityFrameworkCore;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Infrastructure.Persistence.Seeds;

public static class IngredientCatalogSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.IngredientCatalog.AnyAsync()) return;
        var items = GetIngredients();
        context.IngredientCatalog.AddRange(items);
        await context.SaveChangesAsync();
    }

    private static List<IngredientCatalog> GetIngredients() =>
    [
        I("egg_large", "Huevo", "protein", 155, 13, 11, 1.1m, 0, 4.50m, "pieza", 55, 1, true, "supermercado"),
        I("chicken_whole", "Pollo entero", "protein", 239, 27, 14, 0, 0, 75.00m, "kg", 1000, 1, true, "mercado_tradicional"),
        I("chicken_pieces", "Pollo en piezas", "protein", 239, 27, 14, 0, 0, 90.00m, "kg", 1000, 2, true, "supermercado"),
        I("chicken_breast", "Pechuga de pollo", "protein", 165, 31, 3.6m, 0, 0, 140.00m, "kg", 1000, 3, true, "supermercado"),
        I("chicken_thighs", "Muslo de pollo", "protein", 209, 26, 11, 0, 0, 95.00m, "kg", 1000, 2, true, "supermercado"),
        I("ground_beef", "Carne molida res", "protein", 254, 17, 20, 0, 0, 180.00m, "kg", 1000, 3, true, "supermercado"),
        I("ground_beef_basic", "Carne molida regular", "protein", 254, 17, 20, 0, 0, 150.00m, "kg", 1000, 2, true, "mercado_tradicional"),
        I("beef_sirloin", "Sirloin de res", "protein", 180, 28, 7, 0, 0, 280.00m, "kg", 1000, 4, true, "supermercado"),
        I("beef_ribeye", "Rib eye", "protein", 291, 24, 21, 0, 0, 420.00m, "kg", 1000, 5, true, "supermercado"),
        I("beef_stew_cuts", "Carne para guisar", "protein", 192, 23, 11, 0, 0, 160.00m, "kg", 1000, 2, true, "mercado_tradicional"),
        I("pork_basic", "Carne de cerdo regular", "protein", 242, 25, 15, 0, 0, 140.00m, "kg", 1000, 2, true, "mercado_tradicional"),
        I("pork_chops", "Chuleta de cerdo", "protein", 231, 24, 15, 0, 0, 170.00m, "kg", 1000, 3, true, "supermercado"),
        I("pork_shoulder", "Pierna de cerdo", "protein", 267, 18, 21, 0, 0, 180.00m, "kg", 1000, 3, true, "mercado_tradicional"),
        I("tuna_can", "Atún en lata", "protein", 132, 28, 1, 0, 0, 18.00m, "lata", 140, 1, true, "supermercado"),
        I("sardines_can", "Sardinas en lata", "protein", 208, 25, 12, 0, 0, 22.00m, "lata", 125, 1, true, "supermercado"),
        I("fish_basic", "Pescado blanco básico", "protein", 96, 20, 1.7m, 0, 0, 140.00m, "kg", 1000, 3, true, "supermercado"),
        I("salmon_fillet", "Filete de salmón", "protein", 208, 20, 13, 0, 0, 520.00m, "kg", 1000, 5, true, "pescaderia"),
        I("sea_bass", "Robalo", "protein", 97, 18, 2, 0, 0, 380.00m, "kg", 1000, 5, true, "pescaderia"),
        I("prawn", "Camarón", "protein", 99, 24, 0.3m, 0.2m, 0, 420.00m, "kg", 1000, 5, true, "pescaderia"),
        I("tuna_steak", "Atún fresco", "protein", 144, 30, 1, 0, 0, 480.00m, "kg", 1000, 5, true, "pescaderia"),
        I("queso_panela", "Queso panela", "dairy", 290, 18, 21, 3.5m, 0, 140.00m, "kg", 1000, 2, true, "supermercado"),
        I("queso_fresco", "Queso fresco", "dairy", 252, 18, 18, 3.7m, 0, 130.00m, "kg", 1000, 2, true, "mercado_tradicional"),
        I("queso_oaxaca", "Queso Oaxaca", "dairy", 356, 21, 29, 2.4m, 0, 180.00m, "kg", 1000, 3, true, "supermercado"),
        I("crema_mexicana", "Crema mexicana", "dairy", 300, 2.5m, 30, 4, 0, 42.00m, "litro", 1000, 2, true, "supermercado"),
        I("butter_basic", "Mantequilla regular", "fat", 717, 0.85m, 81, 0.06m, 0, 55.00m, "200g", 200, 2, true, "supermercado"),
        I("grass_fed_butter", "Mantequilla orgánica", "fat", 717, 0.85m, 81, 0.06m, 0, 180.00m, "225g", 225, 5, true, "supermercado"),
        I("greek_yogurt", "Yogurt griego natural", "dairy", 97, 9, 5, 4, 0, 55.00m, "500g", 500, 3, true, "supermercado"),
        I("cottage_cheese", "Requesón", "dairy", 98, 11, 4, 3, 0, 90.00m, "kg", 1000, 3, true, "supermercado"),
        I("exotic_cheese", "Queso especial", "dairy", 330, 22, 27, 2, 0, 420.00m, "kg", 1000, 5, true, "supermercado"),
        I("string_cheese", "Queso oaxaca individual", "dairy", 356, 21, 29, 2.4m, 0, 12.00m, "pieza", 28, 3, true, "supermercado"),
        I("whey_protein", "Proteína whey", "protein", 350, 75, 5, 8, 0, 800.00m, "kg", 1000, 4, true, "tienda_especializada"),
        I("egg_whites", "Claras de huevo", "protein", 52, 11, 0.2m, 0.7m, 0, 80.00m, "litro", 1000, 3, true, "supermercado"),
        I("chayote", "Chayote", "vegetable", 19, 0.8m, 0.1m, 4.5m, 1.7m, 18.00m, "kg", 1000, 1, true, "mercado_tradicional"),
        I("calabaza", "Calabaza", "vegetable", 17, 1.2m, 0.3m, 3.1m, 1, 28.00m, "kg", 1000, 1, true, "mercado_tradicional"),
        I("jitomate", "Jitomate", "vegetable", 18, 0.9m, 0.2m, 3.9m, 1.2m, 22.00m, "kg", 1000, 1, true, "mercado_tradicional"),
        I("cebolla", "Cebolla blanca", "vegetable", 40, 1.1m, 0.1m, 9.3m, 1.7m, 18.00m, "kg", 1000, 1, false, "mercado_tradicional"),
        I("aguacate", "Aguacate Hass", "fruit", 160, 2, 15, 8.5m, 6.7m, 75.00m, "kg", 1000, 3, true, "mercado_tradicional"),
        I("chile_serrano", "Chile serrano", "vegetable", 32, 1.7m, 0.4m, 7, 3.7m, 40.00m, "kg", 1000, 1, true, "mercado_tradicional"),
        I("chile_jalapeno", "Chile jalapeño", "vegetable", 29, 0.9m, 0.4m, 6.5m, 2.8m, 32.00m, "kg", 1000, 1, true, "mercado_tradicional"),
        I("lechuga_romana", "Lechuga romana", "vegetable", 17, 1.2m, 0.3m, 3.3m, 2.1m, 25.00m, "pieza", 300, 2, true, "supermercado"),
        I("espinaca", "Espinaca", "vegetable", 23, 2.9m, 0.4m, 3.6m, 2.2m, 30.00m, "manojo", 250, 2, true, "mercado_tradicional"),
        I("brocoli", "Brócoli", "vegetable", 34, 2.8m, 0.4m, 6.6m, 2.6m, 40.00m, "kg", 1000, 2, true, "supermercado"),
        I("coliflor", "Coliflor", "vegetable", 25, 1.9m, 0.3m, 5, 2, 32.00m, "kg", 1000, 2, true, "supermercado"),
        I("pepino", "Pepino", "vegetable", 16, 0.7m, 0.1m, 3.6m, 0.5m, 22.00m, "kg", 1000, 1, true, "mercado_tradicional"),
        I("apio", "Apio", "vegetable", 16, 0.7m, 0.2m, 3, 1.6m, 28.00m, "manojo", 300, 2, true, "supermercado"),
        I("champinones", "Champiñones", "vegetable", 22, 3.1m, 0.3m, 3.3m, 1, 80.00m, "kg", 1000, 2, true, "supermercado"),
        I("rabanito", "Rábano", "vegetable", 16, 0.7m, 0.1m, 3.4m, 1.6m, 18.00m, "manojo", 250, 1, true, "mercado_tradicional"),
        I("nopales", "Nopales", "vegetable", 16, 1.3m, 0.1m, 3.3m, 2.2m, 18.00m, "kg", 1000, 1, true, "mercado_tradicional"),
        I("aceite_oliva", "Aceite de oliva", "fat", 884, 0, 100, 0, 0, 180.00m, "litro", 1000, 3, true, "supermercado"),
        I("aceite_coco", "Aceite de coco", "fat", 862, 0, 100, 0, 0, 150.00m, "500ml", 500, 4, true, "supermercado"),
        I("manteca_cerdo", "Manteca de cerdo", "fat", 897, 0, 100, 0, 0, 75.00m, "kg", 1000, 1, true, "mercado_tradicional"),
        I("mct_oil", "Aceite MCT", "fat", 830, 0, 100, 0, 0, 580.00m, "500ml", 500, 5, true, "tienda_especializada"),
        I("mayonesa", "Mayonesa", "fat", 680, 1, 75, 1, 0, 55.00m, "500g", 500, 2, true, "supermercado"),
        I("chia", "Semillas de chía", "nut_seed", 486, 17, 31, 42, 34, 80.00m, "500g", 500, 2, true, "mercado_tradicional"),
        I("linaza", "Linaza", "nut_seed", 534, 18, 42, 29, 27, 55.00m, "500g", 500, 2, true, "mercado_tradicional"),
        I("almendras", "Almendras", "nut_seed", 579, 21, 49, 22, 12, 260.00m, "500g", 500, 4, true, "supermercado"),
        I("cacahuates", "Cacahuates", "nut_seed", 567, 26, 49, 16, 8.5m, 90.00m, "500g", 500, 2, true, "mercado_tradicional"),
        I("nueces", "Nuez de castilla", "nut_seed", 654, 15, 65, 14, 6.7m, 380.00m, "500g", 500, 4, true, "supermercado"),
        I("macadamia", "Macadamia", "nut_seed", 718, 8, 76, 14, 9, 720.00m, "500g", 500, 5, true, "tienda_especializada"),
        I("pine_nuts", "Piñones", "nut_seed", 673, 14, 68, 13, 3.7m, 1200.00m, "500g", 500, 5, true, "tienda_especializada"),
        I("almond_flour_premium", "Harina de almendra", "nut_seed", 579, 21, 49, 22, 12, 360.00m, "500g", 500, 5, true, "tienda_especializada"),
        I("sal", "Sal", "seasoning", 0, 0, 0, 0, 0, 15.00m, "kg", 1000, 1, true, "supermercado"),
        I("pimienta", "Pimienta negra", "seasoning", 251, 11, 3, 64, 26, 40.00m, "100g", 100, 2, true, "supermercado"),
        I("comino", "Comino", "seasoning", 375, 18, 22, 44, 11, 35.00m, "100g", 100, 2, true, "supermercado"),
        I("oregano", "Orégano", "seasoning", 265, 9, 4.3m, 69, 42, 30.00m, "50g", 50, 2, true, "supermercado"),
        I("ajo", "Ajo", "seasoning", 149, 6.4m, 0.5m, 33, 2.1m, 80.00m, "kg", 1000, 1, true, "mercado_tradicional"),
        I("limon", "Limón", "fruit", 29, 1.1m, 0.3m, 9, 2.8m, 25.00m, "kg", 1000, 1, true, "mercado_tradicional"),
        I("vinagre_manzana", "Vinagre de manzana", "condiment", 22, 0, 0, 0.9m, 0, 40.00m, "500ml", 500, 2, true, "supermercado"),
        I("mostaza", "Mostaza", "condiment", 66, 4, 4, 5, 3, 32.00m, "200g", 200, 2, true, "supermercado")
    ];

    private static IngredientCatalog I(string code, string name, string cat, decimal kcal, decimal prot, decimal fat, decimal carbs, decimal fiber, decimal price, string unit, decimal grams, int tier, bool keto, string store) =>
        new()
        {
            Code = code, Name = name, NameSearch = name.ToLowerInvariant(), Category = cat,
            KcalPer100g = kcal, ProteinGPer100g = prot, FatGPer100g = fat, CarbsGPer100g = carbs, FiberGPer100g = fiber,
            AvgPriceMxn = price, AvgPriceUnit = unit, UnitGramsEquivalent = grams,
            EconomicTier = tier, IsKetoFriendly = keto, PrimaryStoreCategory = store
        };
}
