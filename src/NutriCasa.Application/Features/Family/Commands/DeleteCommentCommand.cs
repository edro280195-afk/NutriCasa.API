using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.Family.Commands;

public record DeleteCommentCommand : IRequest<Result>
{
    public Guid PostId { get; init; }
    public Guid CommentId { get; init; }
}

public class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeleteCommentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteCommentCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUser.UserId.Value;

        var comment = await _context.PostComments
            .Include(c => c.Post)
            .FirstOrDefaultAsync(c => c.Id == request.CommentId && c.PostId == request.PostId && c.DeletedAt == null, ct);

        if (comment is null)
            return Result.Failure("Comentario no encontrado.", "NOT_FOUND");

        if (comment.UserId != userId)
        {
            var membership = await _context.GroupMemberships
                .FirstOrDefaultAsync(m => m.UserId == userId && m.GroupId == comment.Post.GroupId && m.LeftAt == null, ct);

            if (membership is null || (membership.Role != Domain.Enums.GroupRole.Owner && membership.Role != Domain.Enums.GroupRole.Admin))
                return Result.Failure("No tienes permiso para eliminar este comentario.", "FORBIDDEN");
        }

        comment.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
