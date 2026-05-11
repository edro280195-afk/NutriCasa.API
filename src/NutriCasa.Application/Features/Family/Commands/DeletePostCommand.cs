using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.Family.Commands;

public record DeletePostCommand : IRequest<Result>
{
    public Guid PostId { get; init; }
}

public class DeletePostCommandHandler : IRequestHandler<DeletePostCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeletePostCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeletePostCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUser.UserId.Value;

        var post = await _context.GroupPosts
            .FirstOrDefaultAsync(p => p.Id == request.PostId && p.DeletedAt == null, ct);

        if (post is null)
            return Result.Failure("Post no encontrado.", "NOT_FOUND");

        if (post.AuthorUserId != userId)
        {
            var membership = await _context.GroupMemberships
                .FirstOrDefaultAsync(m => m.UserId == userId && m.GroupId == post.GroupId && m.LeftAt == null, ct);

            if (membership is null || (membership.Role != Domain.Enums.GroupRole.Owner && membership.Role != Domain.Enums.GroupRole.Admin))
                return Result.Failure("No tienes permiso para eliminar este post.", "FORBIDDEN");
        }

        post.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
