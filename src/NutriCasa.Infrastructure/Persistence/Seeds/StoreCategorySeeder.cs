using Microsoft.EntityFrameworkCore;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Infrastructure.Persistence.Seeds;

public static class StoreCategorySeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.StoreCategories.AnyAsync()) return;
        context.StoreCategories.AddRange(
            new StoreCategory { Code = "mercado_tradicional", Name = "Mercado Tradicional", ShortDescription = "Mercados municipales y de barrio", IconCode = "storefront", TypicalPriceFactor = 0.85m, SortOrder = 1, GooglePlaceTypes = new[] { "market" } },
            new StoreCategory { Code = "supermercado", Name = "Supermercado", ShortDescription = "Cadenas de supermercados", IconCode = "local_grocery_store", TypicalPriceFactor = 1.0m, SortOrder = 2, GooglePlaceTypes = new[] { "supermarket", "grocery_or_supermarket" } },
            new StoreCategory { Code = "tianguis", Name = "Tianguis", ShortDescription = "Mercados sobre ruedas semanales", IconCode = "festival", TypicalPriceFactor = 0.80m, SortOrder = 3 },
            new StoreCategory { Code = "tienda_esquina", Name = "Tienda de Esquina", ShortDescription = "Abarrotes y misceláneas de barrio", IconCode = "store", TypicalPriceFactor = 1.15m, SortOrder = 4, GooglePlaceTypes = new[] { "convenience_store" } }
        );
        await context.SaveChangesAsync();
    }
}
