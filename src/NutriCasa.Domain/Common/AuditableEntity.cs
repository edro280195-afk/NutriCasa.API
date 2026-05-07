namespace NutriCasa.Domain.Common;

/// <summary>
/// Entidad con campos de auditoría automática.
/// CreatedAt y UpdatedAt se asignan por AuditableEntityInterceptor en SaveChanges.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
