namespace NutriCasa.Domain.Common;

/// <summary>
/// Entidad con borrado lógico (soft delete).
/// El SoftDeleteInterceptor convierte Remove() en UPDATE deleted_at = NOW().
/// Se aplica un filtro global HasQueryFilter(e => e.DeletedAt == null) en la configuración.
/// </summary>
public abstract class SoftDeletableEntity : AuditableEntity
{
    public DateTime? DeletedAt { get; set; }
}
