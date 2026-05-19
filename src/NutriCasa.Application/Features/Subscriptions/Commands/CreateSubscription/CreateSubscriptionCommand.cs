using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Application.Common.Models;
using NutriCasa.Application.Features.Subscriptions.DTOs;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Application.Features.Subscriptions.Commands.CreateSubscription;

public record CreateSubscriptionCommand : IRequest<Result<UserSubscriptionDto>>
{
    public required Guid PlanId { get; init; }
    public bool IsTrial { get; init; }
}

public class CreateSubscriptionCommandHandler : IRequestHandler<CreateSubscriptionCommand, Result<UserSubscriptionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IPaymentService _paymentService;

    public CreateSubscriptionCommandHandler(
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

    public async Task<Result<UserSubscriptionDto>> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
            return Result<UserSubscriptionDto>.Failure("No autenticado.", "UNAUTHORIZED");

        var userId = _currentUserService.UserId.Value;

        var plan = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == request.PlanId && p.IsActive, cancellationToken);

        if (plan is null)
            return Result<UserSubscriptionDto>.Failure("Plan no encontrado.", "NOT_FOUND");

        var now = _dateTimeService.UtcNow;

        // Check for existing active subscription
        var existing = await _context.UserSubscriptions
            .AnyAsync(s => s.UserId == userId
                        && (s.Status == SubscriptionStatus.Active
                         || s.Status == SubscriptionStatus.Trialing
                         || s.Status == SubscriptionStatus.Pending), cancellationToken);

        if (existing)
            return Result<UserSubscriptionDto>.Failure("Ya tienes una suscripción activa.", "CONFLICT");

        // If trial and plan has trial days, check if trial was already used
        if (request.IsTrial && plan.TrialDays > 0)
        {
            var trialUsed = await _context.UserTrialsUsed
                .AnyAsync(t => t.UserId == userId && t.PlanId == request.PlanId && !t.ConvertedToPaid, cancellationToken);

            if (trialUsed)
                return Result<UserSubscriptionDto>.Failure("Ya usaste el período de prueba para este plan.", "TRIAL_USED");
        }

        string? providerSubId = null;
        string? checkoutUrl = null;
        var status = SubscriptionStatus.Active;
        var paymentProvider = plan.PriceMonthlyMxn <= 0 ? "free" : "mercadopago";

        if (request.IsTrial && plan.TrialDays > 0)
        {
            status = SubscriptionStatus.Trialing;
            providerSubId = await _paymentService.CreateTrialSubscriptionAsync(userId, request.PlanId, cancellationToken);

            var trialRecord = new UserTrialUsed
            {
                UserId = userId,
                PlanId = request.PlanId,
                TrialStartedAt = now,
                TrialEndedAt = now.AddDays(plan.TrialDays),
                CreatedAt = now,
            };
            _context.UserTrialsUsed.Add(trialRecord);
        }
        else if (plan.PriceMonthlyMxn <= 0)
        {
            providerSubId = $"free_{userId:N}_{plan.Id:N}";
        }
        else
        {
            status = SubscriptionStatus.Pending;
            var checkout = await _paymentService.CreateCheckoutSessionAsync(userId, request.PlanId, "/profile/subscription?payment=success", cancellationToken);
            providerSubId = checkout.ProviderReference;
            checkoutUrl = checkout.CheckoutUrl;
        }

        var subscription = new UserSubscription
        {
            UserId = userId,
            PlanId = request.PlanId,
            Status = status,
            StartedAt = now,
            CurrentPeriodStart = now,
            CurrentPeriodEnd = request.IsTrial ? now.AddDays(plan.TrialDays) : now.AddMonths(1),
            PaymentProvider = paymentProvider,
            ProviderSubscriptionId = providerSubId,
            Metadata = checkoutUrl is null ? null : System.Text.Json.JsonSerializer.Serialize(new { checkout_url = checkoutUrl }),
        };

        _context.UserSubscriptions.Add(subscription);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new UserSubscriptionDto
        {
            SubscriptionId = subscription.Id,
            PlanId = plan.Id,
            PlanCode = plan.Code,
            PlanName = plan.Name,
            PriceMonthlyMxn = plan.PriceMonthlyMxn,
            Status = subscription.Status.ToString(),
            StartedAt = subscription.StartedAt,
            CurrentPeriodEnd = subscription.CurrentPeriodEnd,
            CancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
            CheckoutUrl = checkoutUrl,
        };

        return Result<UserSubscriptionDto>.Success(dto);
    }
}
