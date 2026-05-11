namespace NutriCasa.Application.Features.Recipes.DTOs;

public record AddFavoriteRequestDto
{
    public required Guid RecipeId { get; init; }
}

public record RemoveFavoriteRequestDto
{
    public required Guid RecipeId { get; init; }
}

public record FavoriteRecipeDto
{
    public required Guid FavoriteId { get; init; }
    public required Guid RecipeId { get; init; }
    public required string RecipeName { get; init; }
    public required int Calories { get; init; }
    public required decimal ProteinGr { get; init; }
    public required decimal FatGr { get; init; }
    public required decimal CarbsGr { get; init; }
    public required string MealType { get; init; }
    public string? PhotoUrl { get; init; }
    public string? Slug { get; init; }
    public required DateTime CreatedAt { get; init; }
}
