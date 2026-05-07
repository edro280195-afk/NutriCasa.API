using NutriCasa.Domain.Common;

namespace NutriCasa.Domain.Entities;

public class RecipeRating : BaseEntity
{
    public Guid RecipeId { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }

    public Recipe Recipe { get; set; } = null!;
    public User User { get; set; } = null!;
}
