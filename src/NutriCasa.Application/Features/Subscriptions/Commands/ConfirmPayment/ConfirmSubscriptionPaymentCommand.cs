using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Subscriptions.DTOs;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.Subscriptions.Commands.ConfirmPayment;

public record ConfirmSubscriptionPaymentCommand : IRequest<Result<UserSubscriptionDto>>
{
    public required string PaymentId { get; init; }
}

public class ConfirmSubscriptionPaymentCommandHandler : IRequestHandler<ConfirmSubscriptionPaymentCommand, Result<UserSubscriptionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IPaymentService _paymentService;

    public ConfirmSubscriptionPaymentCommandHandler(
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

    public async Task<Result<UserSubscriptionDto>> Handle(ConfirmSubscriptionPaymentCommand request, CancellationToken ct)
    {
        if (_currentUserService.UserId is null)
            return Result<UserSubscriptionDto>.Failure("No autenticado.", "UNAUTHORIZED");

        if (string.IsNullOrWhiteSpace(request.PaymentId))
            return Result<UserSubscriptionDto>.Failure("El pago es requerido.", "PAYMENT_ID_REQUIRED");

        var userId = _currentUserService.UserId.Value;
        var isApproved = await _paymentService.VerifyPaymentAsync(request.PaymentId, ct);

        if (!isApproved)
            return Result<UserSubscriptionDto>.Failure("El pago aun no esta aprobado.", "PAYMENT_NOT_APPROVED");

        var subscription = await _context.UserSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Pending)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync(ct);

        if (subscription is null)
            return Result<UserSubscriptionDto>.Failure("No hay una suscripcion pendiente para activar.", "NO_PENDING_SUBSCRIPTION");

        var now = _dateTimeService.UtcNow;
        subscription.Status = SubscriptionStatus.Active;
        subscription.CurrentPeriodStart = now;
        subscription.CurrentPeriodEnd = now.AddMonths(1);
        subscription.ProviderCustomerId = request.PaymentId;
        subscription.Metadata = null;

        await _context.SaveChangesAsync(ct);

        return Result<UserSubscriptionDto>.Success(new UserSubscriptionDto
        {
            SubscriptionId = subscription.Id,
            PlanId = subscription.PlanId,
            PlanCode = subscription.Plan.Code,
            PlanName = subscription.Plan.Name,
            PriceMonthlyMxn = subscription.Plan.PriceMonthlyMxn,
            Status = subscription.Status.ToString(),
            StartedAt = subscription.StartedAt,
            CurrentPeriodEnd = subscription.CurrentPeriodEnd,
            CancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
        });
    }
}
