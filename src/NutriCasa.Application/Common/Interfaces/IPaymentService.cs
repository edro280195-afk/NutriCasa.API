namespace NutriCasa.Application.Common.Interfaces;

public interface IPaymentService
{
    Task<string> CreateSubscriptionAsync(Guid userId, string planCode, CancellationToken ct = default);
    Task CancelSubscriptionAsync(string providerSubscriptionId, CancellationToken ct = default);
}
