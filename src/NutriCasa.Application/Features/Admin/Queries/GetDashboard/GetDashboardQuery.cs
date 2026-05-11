using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Admin.DTOs;

namespace NutriCasa.Application.Features.Admin.Queries.GetDashboard;

public record GetDashboardQuery : IRequest<Result<AdminDashboardDto>>;

public class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, Result<AdminDashboardDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public GetDashboardQueryHandler(IApplicationDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task<Result<AdminDashboardDto>> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var now = _dateTimeService.UtcNow;
        var today = DateOnly.FromDateTime(now);

        var totalUsers = await _context.Users.CountAsync(cancellationToken);
        var activeSubs = await _context.UserSubscriptions
            .CountAsync(s => s.Status == Domain.Enums.SubscriptionStatus.Active, cancellationToken);
        var postsToday = await _context.GroupPosts
            .CountAsync(p => DateOnly.FromDateTime(p.CreatedAt) == today, cancellationToken);
        var newUsersToday = await _context.Users
            .CountAsync(u => DateOnly.FromDateTime(u.CreatedAt) == today, cancellationToken);
        var totalRecipes = await _context.Recipes.CountAsync(cancellationToken);
        var pendingVerif = await _context.Users
            .CountAsync(u => u.EmailVerifiedAt == null && u.DeletedAt == null, cancellationToken);

        var recentPosts = await _context.GroupPosts
            .Include(p => p.AuthorUser)
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new RecentPostDto
            {
                PostId = p.Id,
                Content = p.Content!.Length > 100 ? p.Content.Substring(0, 100) + "..." : p.Content,
                AuthorName = p.AuthorUser!.FullName,
                CreatedAt = p.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var dto = new AdminDashboardDto
        {
            TotalUsers = totalUsers,
            ActiveSubscriptions = activeSubs,
            PostsToday = postsToday,
            NewUsersToday = newUsersToday,
            TotalRecipes = totalRecipes,
            PendingVerifications = pendingVerif,
            RecentPosts = recentPosts,
        };

        return Result<AdminDashboardDto>.Success(dto);
    }
}
