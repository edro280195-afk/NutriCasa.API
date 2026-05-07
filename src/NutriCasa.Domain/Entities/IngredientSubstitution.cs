using NutriCasa.Domain.Common;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Domain.Entities;

public class IngredientSubstitution : AuditableEntity
{
    public Guid OriginalIngredientId { get; set; }
    public Guid ReplacementId { get; set; }
    public string[] ApplicableModeCodes { get; set; } = [];
    public SubstitutionReason Reason { get; set; }
    public decimal? CostSavingsPercent { get; set; }
    public int? QualityLossScore { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public IngredientCatalog OriginalIngredient { get; set; } = null!;
    public IngredientCatalog Replacement { get; set; } = null!;
}
