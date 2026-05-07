using NutriCasa.Domain.Common;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Domain.Entities;

public class ToxicWord : AuditableEntity
{
    public string Word { get; set; } = null!;
    public string NormalizedWord { get; set; } = null!;
    public ToxicWordCategory Category { get; set; }
    public ToxicWordSeverity Severity { get; set; } = ToxicWordSeverity.Medium;
    public string Language { get; set; } = "es-MX";
    public bool IsRegex { get; set; }
    public string? Pattern { get; set; }
    public bool IsActive { get; set; } = true;
}
