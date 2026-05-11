using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Subscriptions.DTOs;

namespace NutriCasa.Application.Features.Subscriptions.Queries.GetMySubscription;

public record GetMySubscriptionQuery : IRequest<Result<UserSubscriptionDto?>>;

public class GetMySubscriptionQueryHandler : IRequestHandler<GetMySubscriptionQuery, Result<UserSubscriptionDto?>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMySubscriptionQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<UserSubscriptionDto?>> Handle(GetMySubscriptionQuery request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
            return Result<UserSubscriptionDto?>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var sub = await _context.UserSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId && s.Status != Domain.Enums.SubscriptionStatus.Expired)
            .OrderByDescending(s => s.StartedAt)
            .Select(s => new UserSubscriptionDto
            {
                SubscriptionId = s.Id,
                PlanId = s.PlanId,
                PlanCode = s.Plan.Code,
                PlanName = s.Plan.Name,
                PriceMonthlyMxn = s.Plan.PriceMonthlyMxn,
                Status = s.Status.ToString(),
                StartedAt = s.StartedAt,
                CurrentPeriodEnd = s.CurrentPeriodEnd,
                CancelAtPeriodEnd = s.CancelAtPeriodEnd,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return Result<UserSubscriptionDto?>.Success(sub);
    }
}
