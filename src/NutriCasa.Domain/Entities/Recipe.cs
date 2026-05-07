using NutriCasa.Domain.Common;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Domain.Entities;

public class Recipe : SoftDeletableEntity
{
    public string Name { get; set; } = null!;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public MealType MealType { get; set; }
    public NutritionTrack NutritionTrack { get; set; } = NutritionTrack.Keto;
    public string Ingredients { get; set; } = "[]"; // JSONB
    public string? Instructions { get; set; }
    public int? PrepTimeMin { get; set; }
    public int? CookTimeMin { get; set; }
    public int Servings { get; set; } = 1;
    public int BaseCalories { get; set; }
    public decimal BaseProteinGr { get; set; }
    public decimal BaseFatGr { get; set; }
    public decimal BaseCarbsGr { get; set; }
    public decimal? BaseNetCarbsGr { get; set; }
    public int? DifficultyLevel { get; set; }
    public string? PhotoUrl { get; set; }
    public string[] Tags { get; set; } = [];
    public RecipeSource Source { get; set; } = RecipeSource.AiGenerated;
    public string? AiModel { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public int UseCount { get; set; }
    public decimal? AvgRating { get; set; }
    public int RatingCount { get; set; }
    public bool IsPublic { get; set; } = true;
    // Del delta 002
    public string[] CompatibleModeCodes { get; set; } = [];
    public int? EconomicTier { get; set; }
    public decimal? EstimatedCostPerServingMxn { get; set; }
    public int? TotalPrepTimeMin { get; set; }
    public int? YieldServingsMin { get; set; }
    public int? YieldServingsMax { get; set; }
    public bool IsBatchCookable { get; set; }
    public bool IsFreezable { get; set; }
    public string[] CookingMethods { get; set; } = [];
    public int? DifficultyScore { get; set; }

    public User? CreatedByUser { get; set; }
    public ICollection<RecipeRating> Ratings { get; set; } = new List<RecipeRating>();
}
