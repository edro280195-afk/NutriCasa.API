namespace NutriCasa.Application.Common.Interfaces;

public interface IPaymentService
{
    Task<string> CreateCheckoutSessionAsync(Guid userId, Guid planId, string returnUrl, CancellationToken ct = default);
    Task<string> CreateTrialSubscriptionAsync(Guid userId, Guid planId, CancellationToken ct = default);
    Task CancelSubscriptionAsync(string providerSubscriptionId, CancellationToken ct = default);
    Task<bool> VerifyPaymentAsync(string providerPaymentId, CancellationToken ct = default);
}
