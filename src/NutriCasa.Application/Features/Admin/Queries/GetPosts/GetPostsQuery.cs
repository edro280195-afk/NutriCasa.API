using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Admin.DTOs;

namespace NutriCasa.Application.Features.Admin.Queries.GetPosts;

public record GetPostsQuery : IRequest<Result<List<AdminPostDto>>>;

public class GetPostsQueryHandler : IRequestHandler<GetPostsQuery, Result<List<AdminPostDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetPostsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<AdminPostDto>>> Handle(GetPostsQuery request, CancellationToken cancellationToken)
    {
        var posts = await _context.GroupPosts
            .Include(p => p.AuthorUser)
            .Include(p => p.Group)
            .Include(p => p.Reactions)
            .OrderByDescending(p => p.CreatedAt)
            .Take(50)
            .Select(p => new AdminPostDto
            {
                PostId = p.Id,
                Content = p.Content!.Length > 120 ? p.Content.Substring(0, 120) + "..." : p.Content,
                AuthorName = p.AuthorUser!.FullName,
                GroupName = p.Group.Name,
                ReactionCount = p.Reactions.Count,
                CreatedAt = p.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return Result<List<AdminPostDto>>.Success(posts);
    }
}
