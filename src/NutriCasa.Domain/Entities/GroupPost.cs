using NutriCasa.Domain.Common;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Domain.Entities;

public class GroupPost : SoftDeletableEntity
{
    public Guid GroupId { get; set; }
    public Guid? AuthorUserId { get; set; }
    public PostType PostType { get; set; }
    public string? Content { get; set; }
    public string? Metadata { get; set; } // JSONB
    public bool IsPinned { get; set; }
    public bool IsAnnouncement { get; set; }
    public bool IsUnderReview { get; set; }
    public string? ModerationReason { get; set; }
    public ToxicWordSeverity? ModerationSeverity { get; set; }
    public DateTime? ModerationResolvedAt { get; set; }
    public ModerationAction? ModerationAction { get; set; }
    public Guid? ModeratedByUserId { get; set; }

    public Group Group { get; set; } = null!;
    public User? AuthorUser { get; set; }
    public User? ModeratedByUser { get; set; }
    public ICollection<PostReaction> Reactions { get; set; } = new List<PostReaction>();
    public ICollection<PostComment> Comments { get; set; } = new List<PostComment>();
}
