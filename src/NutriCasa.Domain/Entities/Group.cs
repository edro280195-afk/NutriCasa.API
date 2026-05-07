using NutriCasa.Domain.Common;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Domain.Entities;

public class Group : SoftDeletableEntity
{
    public Guid? ParentGroupId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string InviteCode { get; set; } = null!;
    public DateTime? InviteCodeExpiresAt { get; set; }
    public GroupType GroupType { get; set; } = GroupType.Household;
    public string? AvatarUrl { get; set; }
    public string? ColorTheme { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public bool IsFrozen { get; set; }

    public Group? ParentGroup { get; set; }
    public User? CreatedByUser { get; set; }
    public ICollection<Group> ChildGroups { get; set; } = new List<Group>();
    public ICollection<GroupMembership> Memberships { get; set; } = new List<GroupMembership>();
}
