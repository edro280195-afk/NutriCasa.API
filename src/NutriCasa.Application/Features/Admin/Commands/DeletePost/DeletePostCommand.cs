using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.Admin.Commands.DeletePost;

public record DeletePostCommand : IRequest<Result>
{
    public required Guid PostId { get; init; }
}

public class DeletePostCommandHandler : IRequestHandler<DeletePostCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeletePostCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(DeletePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _context.GroupPosts
            .Include(p => p.Reactions)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == request.PostId, cancellationToken);

        if (post is null)
            return Result.Failure("Publicación no encontrada.", "NOT_FOUND");

        _context.PostReactions.RemoveRange(post.Reactions);
        _context.PostComments.RemoveRange(post.Comments);
        _context.GroupPosts.Remove(post);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
