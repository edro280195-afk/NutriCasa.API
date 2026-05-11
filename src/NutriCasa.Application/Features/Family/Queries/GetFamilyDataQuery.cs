using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;

namespace NutriCasa.Application.Features.Family.Queries;

public record GetFamilyMembersQuery : IRequest<Result<List<FamilyMemberDto>>>;
public record GetFamilyFeedQuery : IRequest<Result<List<FamilyPostDto>>>;
public record GetFamilyStatsQuery : IRequest<Result<FamilyStatsDto>>;

public class GetFamilyMembersQueryHandler : IRequestHandler<GetFamilyMembersQuery, Result<List<FamilyMemberDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetFamilyMembersQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<FamilyMemberDto>>> Handle(GetFamilyMembersQuery request, CancellationToken ct)
    {
        if (_currentUserService.UserId is null)
            return Result<List<FamilyMemberDto>>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var membership = await _context.GroupMemberships
            .Include(m => m.Group)
            .FirstOrDefaultAsync(m => m.UserId == userId && m.LeftAt == null, ct);

        if (membership is null)
            return Result<List<FamilyMemberDto>>.Failure("No perteneces a ningún grupo.", "NO_GROUP");

        var members = await _context.GroupMemberships
            .Include(m => m.User)
            .Where(m => m.GroupId == membership.GroupId && m.LeftAt == null)
            .Select(m => new FamilyMemberDto
            {
                UserId = m.UserId,
                FullName = m.Nickname ?? m.User.FullName,
                Role = m.Role.ToString().ToLowerInvariant(),
                JoinedAt = m.JoinedAt,
            })
            .ToListAsync(ct);

        return Result<List<FamilyMemberDto>>.Success(members);
    }
}

public class GetFamilyFeedQueryHandler : IRequestHandler<GetFamilyFeedQuery, Result<List<FamilyPostDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetFamilyFeedQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<FamilyPostDto>>> Handle(GetFamilyFeedQuery request, CancellationToken ct)
    {
        if (_currentUserService.UserId is null)
            return Result<List<FamilyPostDto>>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var membership = await _context.GroupMemberships
            .FirstOrDefaultAsync(m => m.UserId == userId && m.LeftAt == null, ct);

        if (membership is null)
            return Result<List<FamilyPostDto>>.Success([]);

        var posts = await _context.GroupPosts
            .Include(p => p.AuthorUser)
            .Include(p => p.Reactions)
                .ThenInclude(r => r.User)
            .Include(p => p.Comments.Where(c => c.DeletedAt == null))
                .ThenInclude(c => c.User)
            .Where(p => p.GroupId == membership.GroupId && p.DeletedAt == null && !p.IsUnderReview)
            .OrderByDescending(p => p.CreatedAt)
            .Take(20)
            .ToListAsync(ct);

        var currentUserId = userId;

        var result = posts.Select(p => new FamilyPostDto
        {
            PostId = p.Id,
            AuthorUserId = p.AuthorUserId,
            AuthorName = p.AuthorUser?.FullName ?? "Alguien",
            PostType = p.PostType.ToString().ToLowerInvariant(),
            Content = p.Content ?? "",
            CreatedAt = p.CreatedAt,
            Reactions = p.Reactions
                .GroupBy(r => r.ReactionType)
                .Select(g => new PostReactionDto
                {
                    Type = g.Key.ToString().ToLowerInvariant(),
                    Count = g.Count(),
                    HasCurrentUserReacted = g.Any(r => r.UserId == currentUserId),
                    Users = g.Select(r => r.User.FullName).Take(3).ToList(),
                })
                .ToList(),
            Comments = p.Comments
                .Where(c => c.DeletedAt == null)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new PostCommentDto
                {
                    CommentId = c.Id,
                    UserId = c.UserId,
                    AuthorName = c.User?.FullName ?? "Alguien",
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    IsOwner = c.UserId == currentUserId,
                })
                .ToList(),
            CommentCount = p.Comments.Count(c => c.DeletedAt == null),
        })
        .ToList();

        return Result<List<FamilyPostDto>>.Success(result);
    }
}

public class GetFamilyStatsQueryHandler : IRequestHandler<GetFamilyStatsQuery, Result<FamilyStatsDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetFamilyStatsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<FamilyStatsDto>> Handle(GetFamilyStatsQuery request, CancellationToken ct)
    {
        if (_currentUserService.UserId is null)
            return Result<FamilyStatsDto>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var membership = await _context.GroupMemberships
            .Include(m => m.Group)
            .FirstOrDefaultAsync(m => m.UserId == userId && m.LeftAt == null, ct);

        if (membership is null || membership.Group is null)
            return Result<FamilyStatsDto>.Success(new FamilyStatsDto());

        var totalMembers = await _context.GroupMemberships
            .CountAsync(m => m.GroupId == membership.GroupId && m.LeftAt == null, ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var groupUserIds = await _context.GroupMemberships
            .Where(m => m.GroupId == membership.GroupId && m.LeftAt == null)
            .Select(m => m.UserId)
            .ToListAsync(ct);

        var activeToday = await _context.DailyCheckIns
            .CountAsync(c => groupUserIds.Contains(c.UserId) && c.CheckInDate == today, ct);

        var since = today.AddDays(-6);
        var weeklyCheckIns = await _context.DailyCheckIns
            .CountAsync(c => c.CheckInDate >= since, ct);

        var adherence = totalMembers > 0
            ? (int)(weeklyCheckIns / (decimal)(totalMembers * 7) * 100)
            : 0;

        return Result<FamilyStatsDto>.Success(new FamilyStatsDto
        {
            TotalMembers = totalMembers,
            ActiveToday = activeToday,
            DailyStreak = weeklyCheckIns,
            AdherencePercent = adherence,
            GroupName = membership.Group.Name,
            DaysActive = (DateTime.UtcNow - membership.Group.CreatedAt).Days,
        });
    }
}

public class FamilyMemberDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Role { get; set; } = "member";
    public DateTime JoinedAt { get; set; }
}

public class FamilyPostDto
{
    public Guid PostId { get; set; }
    public Guid? AuthorUserId { get; set; }
    public string AuthorName { get; set; } = null!;
    public string PostType { get; set; } = null!;
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public List<PostReactionDto> Reactions { get; set; } = new();
    public List<PostCommentDto> Comments { get; set; } = new();
    public int CommentCount { get; set; }
}

public class PostReactionDto
{
    public string Type { get; set; } = "";
    public int Count { get; set; }
    public bool HasCurrentUserReacted { get; set; }
    public List<string> Users { get; set; } = new();
}

public class PostCommentDto
{
    public Guid CommentId { get; set; }
    public Guid UserId { get; set; }
    public string AuthorName { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool IsOwner { get; set; }
}

public class FamilyStatsDto
{
    public int TotalMembers { get; set; }
    public int ActiveToday { get; set; }
    public int DailyStreak { get; set; }
    public int AdherencePercent { get; set; }
    public string GroupName { get; set; } = "Mi Familia";
    public int DaysActive { get; set; }
}
