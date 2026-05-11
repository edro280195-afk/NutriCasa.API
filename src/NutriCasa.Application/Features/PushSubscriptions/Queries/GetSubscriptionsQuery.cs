using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.PushSubscriptions.DTOs;

namespace NutriCasa.Application.Features.PushSubscriptions.Queries;

public record GetSubscriptionsQuery : IRequest<Result<List<PushSubscriptionDto>>>;

public class GetSubscriptionsQueryHandler : IRequestHandler<GetSubscriptionsQuery, Result<List<PushSubscriptionDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetSubscriptionsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<List<PushSubscriptionDto>>> Handle(GetSubscriptionsQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result<List<PushSubscriptionDto>>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUser.UserId.Value;

        var subs = await _context.PushSubscriptions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastUsedAt)
            .Select(s => new PushSubscriptionDto
            {
                Id = s.Id,
                Endpoint = s.Endpoint,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                LastUsedAt = s.LastUsedAt,
            })
            .ToListAsync(ct);

        return Result<List<PushSubscriptionDto>>.Success(subs);
    }
}
