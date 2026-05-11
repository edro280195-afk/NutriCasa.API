using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Domain.Entities;

namespace NutriCasa.Application.Features.Family.Commands;

public record AddCommentCommand : IRequest<Result<CommentResultDto>>
{
    public Guid PostId { get; init; }
    public string Content { get; init; } = "";
}

public record CommentResultDto
{
    public Guid CommentId { get; set; }
    public Guid UserId { get; set; }
    public string AuthorName { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, Result<CommentResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AddCommentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<CommentResultDto>> Handle(AddCommentCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<CommentResultDto>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUser.UserId.Value;

        var post = await _context.GroupPosts
            .FirstOrDefaultAsync(p => p.Id == request.PostId && p.DeletedAt == null, ct);

        if (post is null)
            return Result<CommentResultDto>.Failure("Post no encontrado.", "NOT_FOUND");

        if (string.IsNullOrWhiteSpace(request.Content))
            return Result<CommentResultDto>.Failure("El comentario no puede estar vacío.", "EMPTY_COMMENT");

        var comment = new PostComment
        {
            PostId = request.PostId,
            UserId = userId,
            Content = request.Content.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _context.PostComments.Add(comment);
        await _context.SaveChangesAsync(ct);

        var user = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(ct);

        return Result<CommentResultDto>.Success(new CommentResultDto
        {
            CommentId = comment.Id,
            UserId = userId,
            AuthorName = user ?? "Alguien",
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
        });
    }
}
