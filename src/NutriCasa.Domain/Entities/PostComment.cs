using NutriCasa.Domain.Common;

namespace NutriCasa.Domain.Entities;

public class PostComment : SoftDeletableEntity
{
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = null!;
    public Guid? ParentCommentId { get; set; }

    public GroupPost Post { get; set; } = null!;
    public User User { get; set; } = null!;
    public PostComment? ParentComment { get; set; }
    public ICollection<PostComment> Replies { get; set; } = new List<PostComment>();
}
