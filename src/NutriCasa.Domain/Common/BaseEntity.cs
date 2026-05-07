namespace NutriCasa.Domain.Common;

/// <summary>
/// Entidad base del dominio. Todas las entidades heredan de aquí.
/// El Id se mapea al PK específico de cada tabla (user_id, plan_id, etc.) en la configuración de EF Core.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
}
