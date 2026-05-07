using NutriCasa.Domain.Common;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Domain.Entities;

public class AiInteraction : BaseEntity
{
    public Guid? UserId { get; set; }
    public Guid? ThreadId { get; set; }
    public AiInteractionType InteractionType { get; set; }
    public string PromptVersion { get; set; } = null!;
    public string ModelUsed { get; set; } = null!;
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public decimal? EstimatedCostUsd { get; set; }
    public int? DurationMs { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PromptHash { get; set; }
    public string? RequestPayload { get; set; } // JSONB
    public string? ResponsePayload { get; set; } // JSONB
    public bool CacheHit { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
}
