using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.Subscriptions.Commands.CancelSubscription;

public record CancelSubscriptionCommand : IRequest<Result>
{
    public bool CancelImmediately { get; init; }
}

public class CancelSubscriptionCommandHandler : IRequestHandler<CancelSubscriptionCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IPaymentService _paymentService;

    public CancelSubscriptionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService,
        IPaymentService paymentService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
        _paymentService = paymentService;
    }

    public async Task<Result> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
            return Result.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var sub = await _context.UserSubscriptions
            .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);

        if (sub is null)
            return Result.Failure("No tienes una suscripción activa.", "NOT_FOUND");

        if (request.CancelImmediately)
        {
            sub.Status = SubscriptionStatus.Cancelled;
            sub.CancelledAt = _dateTimeService.UtcNow;
        }
        else
        {
            sub.CancelAtPeriodEnd = true;
        }

        if (!string.IsNullOrWhiteSpace(sub.ProviderSubscriptionId))
        {
            await _paymentService.CancelSubscriptionAsync(sub.ProviderSubscriptionId, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
