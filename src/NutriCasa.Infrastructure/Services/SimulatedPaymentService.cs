using NutriCasa.Application.Common.Interfaces;

namespace NutriCasa.Infrastructure.Services;

public class SimulatedPaymentService : IPaymentService
{
    private static int _counter;

    public Task<PaymentCheckoutResult> CreateCheckoutSessionAsync(Guid userId, Guid planId, string returnUrl, CancellationToken ct = default)
    {
        var sessionId = $"pay_sim_{Interlocked.Increment(ref _counter):x8}";
        return Task.FromResult(new PaymentCheckoutResult(sessionId, returnUrl));
    }

    public Task<string> CreateTrialSubscriptionAsync(Guid userId, Guid planId, CancellationToken ct = default)
    {
        var subId = $"trial_sim_{Interlocked.Increment(ref _counter):x8}";
        return Task.FromResult(subId);
    }

    public Task CancelSubscriptionAsync(string providerSubscriptionId, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task<bool> VerifyPaymentAsync(string providerPaymentId, CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }
}
