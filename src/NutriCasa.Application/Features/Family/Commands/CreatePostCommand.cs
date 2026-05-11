using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.Family.Commands;

public record CreatePostCommand : IRequest<Result<PostResultDto>>
{
    public string Content { get; init; } = "";
    public string PostType { get; init; } = "UserText";
}

public record PostResultDto
{
    public Guid PostId { get; set; }
    public string AuthorName { get; set; } = "";
    public string PostType { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, Result<PostResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreatePostCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<PostResultDto>> Handle(CreatePostCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<PostResultDto>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUser.UserId.Value;

        var membership = await _context.GroupMemberships
            .Include(m => m.Group)
            .FirstOrDefaultAsync(m => m.UserId == userId && m.LeftAt == null, ct);

        if (membership is null)
            return Result<PostResultDto>.Failure("No perteneces a ningún grupo.", "NO_GROUP");

        if (!Enum.TryParse<PostType>(request.PostType, true, out var postType))
            postType = PostType.UserText;

        var post = new GroupPost
        {
            GroupId = membership.GroupId,
            AuthorUserId = userId,
            PostType = postType,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _context.GroupPosts.Add(post);
        await _context.SaveChangesAsync(ct);

        var user = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(ct);

        return Result<PostResultDto>.Success(new PostResultDto
        {
            PostId = post.Id,
            AuthorName = user ?? "Alguien",
            PostType = postType.ToString().ToLowerInvariant(),
            Content = post.Content ?? "",
            CreatedAt = post.CreatedAt,
        });
    }
}
