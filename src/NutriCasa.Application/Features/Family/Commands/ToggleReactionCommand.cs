using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.Family.Commands;

public record ToggleReactionCommand : IRequest<Result<ReactionResultDto>>
{
    public Guid PostId { get; init; }
    public string ReactionType { get; init; } = "Like";
}

public record ReactionResultDto
{
    public bool HasReacted { get; set; }
    public string ReactionType { get; set; } = "";
    public int Count { get; set; }
}

public class ToggleReactionCommandHandler : IRequestHandler<ToggleReactionCommand, Result<ReactionResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ToggleReactionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<ReactionResultDto>> Handle(ToggleReactionCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<ReactionResultDto>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUser.UserId.Value;

        var post = await _context.GroupPosts
            .FirstOrDefaultAsync(p => p.Id == request.PostId && p.DeletedAt == null, ct);

        if (post is null)
            return Result<ReactionResultDto>.Failure("Post no encontrado.", "NOT_FOUND");

        if (!Enum.TryParse<ReactionType>(request.ReactionType, true, out var reactionType))
            return Result<ReactionResultDto>.Failure("Tipo de reacción inválido.", "INVALID_REACTION");

        var existing = await _context.PostReactions
            .FirstOrDefaultAsync(r => r.PostId == request.PostId && r.UserId == userId && r.ReactionType == reactionType, ct);

        if (existing is not null)
        {
            _context.PostReactions.Remove(existing);
            await _context.SaveChangesAsync(ct);

            var count = await _context.PostReactions
                .CountAsync(r => r.PostId == request.PostId && r.ReactionType == reactionType, ct);

            return Result<ReactionResultDto>.Success(new ReactionResultDto
            {
                HasReacted = false,
                ReactionType = request.ReactionType,
                Count = count,
            });
        }

        var reaction = new PostReaction
        {
            PostId = request.PostId,
            UserId = userId,
            ReactionType = reactionType,
            CreatedAt = DateTime.UtcNow,
        };

        _context.PostReactions.Add(reaction);
        await _context.SaveChangesAsync(ct);

        var newCount = await _context.PostReactions
            .CountAsync(r => r.PostId == request.PostId && r.ReactionType == reactionType, ct);

        return Result<ReactionResultDto>.Success(new ReactionResultDto
        {
            HasReacted = true,
            ReactionType = request.ReactionType,
            Count = newCount,
        });
    }
}
