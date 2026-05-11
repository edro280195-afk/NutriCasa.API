using NutriCasa.Domain.Common;

namespace NutriCasa.Domain.Entities;

public class FavoriteRecipe : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid RecipeId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Recipe Recipe { get; set; } = null!;
}
