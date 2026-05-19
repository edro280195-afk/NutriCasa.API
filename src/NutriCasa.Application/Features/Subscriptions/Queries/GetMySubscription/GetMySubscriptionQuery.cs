using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
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
            .FirstOrDefaultAsync(cancellationToken);

        if (sub is null)
            return Result<UserSubscriptionDto?>.Success(null);

        return Result<UserSubscriptionDto?>.Success(new UserSubscriptionDto
        {
            SubscriptionId = sub.Id,
            PlanId = sub.PlanId,
            PlanCode = sub.Plan.Code,
            PlanName = sub.Plan.Name,
            PriceMonthlyMxn = sub.Plan.PriceMonthlyMxn,
            Status = sub.Status.ToString(),
            StartedAt = sub.StartedAt,
            CurrentPeriodEnd = sub.CurrentPeriodEnd,
            CancelAtPeriodEnd = sub.CancelAtPeriodEnd,
            CheckoutUrl = GetCheckoutUrl(sub.Metadata),
        });
    }

    private static string? GetCheckoutUrl(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(metadata);
            return doc.RootElement.TryGetProperty("checkout_url", out var value)
                ? value.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
