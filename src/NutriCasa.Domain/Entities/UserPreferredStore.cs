using NutriCasa.Domain.Common;

namespace NutriCasa.Domain.Entities;

public class UserPreferredStore : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid StoreCategoryId { get; set; }
    public string? CustomStoreName { get; set; }
    public string? GooglePlaceId { get; set; }
    public string? VisitFrequency { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public User User { get; set; } = null!;
    public StoreCategory StoreCategory { get; set; } = null!;
}
