using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Notifications.DTOs;

namespace NutriCasa.Application.Features.Notifications.Queries;

public record GetNotificationsQuery : IRequest<Result<List<NotificationDto>>>
{
    public int Take { get; init; } = 50;
    public int Skip { get; init; } = 0;
}

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, Result<List<NotificationDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetNotificationsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<List<NotificationDto>>> Handle(GetNotificationsQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<List<NotificationDto>>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUser.UserId.Value;

        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Body = n.Body,
                DeepLink = n.DeepLink,
                IconUrl = n.IconUrl,
                Metadata = n.Metadata,
                CreatedAt = n.CreatedAt,
                ReadAt = n.ReadAt,
            })
            .ToListAsync(ct);

        return Result<List<NotificationDto>>.Success(notifications);
    }
}
