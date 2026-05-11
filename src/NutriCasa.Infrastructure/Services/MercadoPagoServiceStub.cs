using NutriCasa.Application.Common.Interfaces;
using NutriCasa.Domain.Entities;
using NutriCasa.Domain.Enums;

namespace NutriCasa.Infrastructure.Services;

public class MercadoPagoServiceStub : IPaymentService
{
    private static int _counter;

    public Task<string> CreateCheckoutSessionAsync(Guid userId, Guid planId, string returnUrl, CancellationToken ct = default)
    {
        var sessionId = $"mp_sim_{Interlocked.Increment(ref _counter):x8}";
        return Task.FromResult(sessionId);
    }

    public Task<string> CreateTrialSubscriptionAsync(Guid userId, Guid planId, CancellationToken ct = default)
    {
        var subId = $"mp_sub_sim_{Interlocked.Increment(ref _counter):x8}";
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
