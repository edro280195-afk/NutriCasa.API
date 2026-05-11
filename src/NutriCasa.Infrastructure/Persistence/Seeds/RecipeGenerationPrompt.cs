namespace NutriCasa.Infrastructure.Persistence.Seeds;

public static class RecipeGenerationPrompt
{
    /// <summary>
    /// Construye el prompt para generar recetas curadas con Gemini Pro.
    /// Genera 15 recetas por modo de presupuesto para un total de 90 recetas.
    /// CompatibleModeCodes se asigna según el modo objetivo.
    /// </summary>
    public static string Build(int recipesPerMode = 15)
    {
        return $@"Eres un chef nutricionista experto en cocina cetogénica mexicana. Genera {recipesPerMode} recetas para CADA UNO de los 6 modos de presupuesto de NutriCasa, produciendo un total de {recipesPerMode * 6} recetas.

## LOS 6 MODOS DE PRESUPUESTO

| Modo | Rango costo/sem | Proteínas preferidas | Reglas especiales |
|------|----------------|---------------------|-------------------|
| economic | $400-550 | Huevo, sardinas, atún, pollo entero, queso panela, nopales | Máximo $35/platillo, 45 min preparación, ingredientes de mercado/tianguis |
| pantry_basic | $550-750 | Pollo en piezas, carne molida, queso fresco, cerdo | Máximo $50/platillo, ingredientes de supermercado |
| simple_kitchen | $550-750 | Huevo, atún, pollo, queso panela | Máximo 20 min, max 6 ingredientes, solo estufa/microondas/licuadora |
| busy_parent | $700-900 | Pollo, res, cerdo, queso | Batch cooking, min 4 porciones, congelable, preparación dominical max 2h |
| athletic | $900-1200 | Pechuga, sirloin, whey protein, atún, camarón | 1.6g proteína/kg, min 25g proteína/platillo, incluir shakes |
| gourmet | $1200-1800 | Salmón, mariscos, ribeye, cordero, quesos premium | Máximo $150/platillo, min 3 cocinas distintas, min 6 proteínas distintas |

## FORMATO DE CADA RECETA (JSON)

Cada receta debe seguir EXACTAMENTE esta estructura:

```json
{{
  ""name"": ""Nombre de la receta"",
  ""slug"": ""nombre-receta"",
  ""description"": ""Descripción corta y atractiva"",
  ""mealType"": ""Breakfast|Lunch|Dinner|Snack"",
  ""ingredients"": [
    {{
      ""code"": ""codigo_ingrediente"",
      ""name"": ""Nombre ingrediente"",
      ""amount"": 100,
      ""unit"": ""g""
    }}
  ],
  ""instructions"": ""Instrucciones paso a paso separadas por \\n"",
  ""prepTimeMin"": 10,
  ""cookTimeMin"": 20,
  ""servings"": 2,
  ""baseCalories"": 450,
  ""baseProteinGr"": 35,
  ""baseFatGr"": 30,
  ""baseCarbsGr"": 8,
  ""baseNetCarbsGr"": 4,
  ""difficultyScore"": 2,
  ""tags"": [""mexican"", ""breakfast""],
  ""compatibleModeCodes"": [""economic""],
  ""economicTier"": 1,
  ""estimatedCostPerServingMxn"": 25.00,
  ""totalPrepTimeMin"": 30,
  ""yieldServingsMin"": 2,
  ""yieldServingsMax"": 4,
  ""isBatchCookable"": false,
  ""isFreezable"": false,
  ""cookingMethods"": [""stovetop""]
}}

## REGLAS DE VALIDEZ

1. Net carbs = Total carbs - Fiber. Máximo 10g net carbs por porción.
2. Proteína mínima 15g por porción (25g para modo athletic).
3. Las recetas deben ser auténticamente mexicanas usando ingredientes locales.
4. Para modo economic usa ingredientes de tier 1-2 (huevo, sardinas, nopales, chayote, etc.)
5. Para modo gourmet usa ingredientes de tier 4-5 (salmón, camarón, ribeye, MCT, macadamia)
6. INCLUYE TODAS las recetas en un SOLO array JSON válido.
7. Cada modo debe tener 4 tipos de comida representados (desayuno, comida, cena, snack).
8. Las recetas deben ser variadas sin repetir platillos entre modos.

## INGREDIENTES DISPONIBLES (usa SOLO estos códigos)

Proteínas: egg_large, chicken_whole, chicken_pieces, chicken_breast, chicken_thighs, chicken_wings, chicken_livers, ground_beef, ground_beef_basic, beef_sirloin, beef_ribeye, beef_stew_cuts, beef_ribs, beef_liver, beef_tongue, beef_tripe, pork_basic, pork_chops, pork_shoulder, pork_ribs, ground_pork, bacon, chorizo, longaniza, pork_rinds, tuna_can, sardines_can, fish_basic, salmon_fillet, sea_bass, prawn, tuna_steak, whey_protein, egg_whites

Lácteos: queso_panela, queso_fresco, queso_oaxaca, crema_mexicana, butter_basic, grass_fed_butter, greek_yogurt, cottage_cheese, exotic_cheese, string_cheese, heavy_cream, cream_cheese, mozzarella, cheddar, parmesan, manchego

Verduras: chayote, calabaza, jitomate, cebolla, aguacate, chile_serrano, chile_jalapeno, lechuga_romana, espinaca, brocoli, coliflor, pepino, apio, champinones, rabanito, nopales, calabacita, pimiento_morron, col_repollo, ejotes, esparragos, jitomate_cherry, berenjena

Frutas: fresa, arandano, coco (solo keto-amigables)

Grasas: aceite_oliva, aceite_coco, manteca_cerdo, mct_oil, mayonesa, aceite_aguacate, leche_coco, crema_coco

Semillas: chia, linaza, almendras, cacahuates, nueces, macadamia, pine_nuts, almond_flour_premium, pepita, harina_coco

Especias: sal, pimienta, comino, oregano, ajo, canela, cacao_polvo, polvo_hornear, vainilla, psyllium, eritritol, stevia, grenetina

Condimentos: vinagre_manzana, mostaza

Otros: limon, cafe, cilantro, epazote

Genera {recipesPerMode} recetas para cada modo en este orden: economic, pantry_basic, simple_kitchen, busy_parent, athletic, gourmet.
RESPONDE ÚNICAMENTE CON EL JSON ARRAY. Sin markdown ni texto adicional fuera del array JSON.
";
    }
}
